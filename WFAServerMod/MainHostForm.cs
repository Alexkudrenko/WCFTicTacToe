using System.ServiceModel;
using System.Windows.Forms;
using GameInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WFAServer
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Single, InstanceContextMode = InstanceContextMode.Single)]
    public partial class MainHostForm : Form, IGameServer
    {
        #region ServerMessages
        string _message_WaitForOthers = "Waiting for other connections";
        string _message_ServerIsFull = "Sorry, server is full! Try to connect later!";
        string _message_DisconnectFromServer = "You have been disconnected from server!";
        string _message_CriticalServerError = "Server closed connection";
        #endregion
        ServiceHost _host;
        Random _randomize = new Random();
        short _gameCount = 0;
        class GameInfo
        {
            IGameClient[] _clients = new IGameClient[2];
            string[] _names = new string[2];
            public IGameClient FirstClient { get{ return _clients[0]; } private set {_clients[0] = value; } }
            public IGameClient SecondClient { get { return _clients[1]; } private set {_clients[1] = value; } }
            public string FirstGamerName {get{return _names[0]; } private set {_names[0] = value; } }
            public string SecondGamerName { get { return _names[1]; } private set {_names[1] = value; } }
            public int GameId;
            public GameInfo(Dictionary<IGameClient, string> loggedGamers, int gameId)
            {
                _clients = loggedGamers.Keys.ToArray();
                _names = loggedGamers.Values.ToArray();
                GameId = gameId;
            }
            public string GetPlayerName(IGameClient client)
            {
                return client == FirstClient ? FirstGamerName : SecondGamerName;
            }
            public IGameClient GetOpponent(IGameClient gameClient)
            {
                if (_clients.Contains(gameClient))
                    return _clients.FirstOrDefault(x => x != gameClient);
                else
                    throw new Exception("Critical error! Opponent wasn't found!");
            }
        }
        GameInfo[] _runningGames = new GameInfo[20];
        Dictionary<IGameClient, string> _loggedGamers = new Dictionary<IGameClient, string>();

        public MainHostForm(ref ServiceHost host)
        {
            InitializeComponent();
            _host = host;
            FormClosed += (sender, args) =>
            {
                if (_gameCount > 0)
                {
                    Parallel.ForEach(_runningGames.Where(x => x != null), x =>
                    {
                        x.FirstClient.StopGame(_message_CriticalServerError, true);
                        x.SecondClient.StopGame(_message_CriticalServerError, true);
                    });
                }
                if (_loggedGamers.Count > 0)
                    _loggedGamers.Keys.ToArray()[0].StopGame(_message_CriticalServerError, true);
            };
            StartServer();
        }
        void SendToLog(string message)
        {
                logTextBox.Text += message;
        }
        void StartServer()
        {
            if (_host == null || _host.State == CommunicationState.Closed)
            {
                _host = new ServiceHost(this);
                _host.Open();
                SendToLog("Server started!");
            }
        }
        public void ConnectToServer(string name)
        {
            var newClient = OperationContext.Current.GetCallbackChannel<IGameClient>();
            if (_gameCount == _runningGames.Length)
            {
                 SendToLog(String.Format("\r\nERROR! Cannot connect {0}, server is full!", name));
                //SendToLog($"\r\nERROR! Cannot connect {name}, server is full!");
                newClient.ReceiveServerMessage(_message_ServerIsFull);
                return;
            }
            _loggedGamers[newClient] = name;
            //SendToLog($"\r\nAdded {name} to gamers list");
            SendToLog(String.Format("\r\nAdded {0} to gamers list", name));
            if (_loggedGamers.Count < 2)
                newClient.ReceiveServerMessage(_message_WaitForOthers);
            else
                StartNewGame();
        }
        void StartNewGame()
        {
            var gameId = SetNewGameId();
            _runningGames[gameId] = new GameInfo(_loggedGamers, gameId);
            SendDataToOpponents(gameId);
            _loggedGamers.Clear();
            _gameCount++;
        }
        void SendDataToOpponents(int gameId, bool isResetGame = false)
        {
            //SendToLog($"\r\nStarting new game. Id = {gameId}. " +
            //                $"{_runningGames[gameId].FirstGamerName} vs {_runningGames[gameId].SecondGamerName}");
            SendToLog(string.Format("\r\nStarting new game. Id = {0}. {1} vs {2}", 
                gameId, _runningGames[gameId].FirstGamerName, _runningGames[gameId].SecondGamerName));
            bool isFirstMove = _randomize.Next(0, 100) < 50 ? true : false;

            var task = Task.Factory.StartNew(() =>
            {
                for (int i = 3; i >= 1; --i)
                {
                    _runningGames[gameId].FirstClient.ReceiveServerMessage(string.Format("Game starts in {0} seconds!", i));
                    _runningGames[gameId].SecondClient.ReceiveServerMessage(string.Format("Game starts in {0} seconds!", i));
                    //_runningGames[gameId].FirstClient.ReceiveServerMessage($"Game starts in {i} seconds!");
                    //_runningGames[gameId].SecondClient.ReceiveServerMessage($"Game starts in {i} seconds!");
                    Thread.Sleep(1000);
                }
                _runningGames[gameId].FirstClient.ReceiveGameData(isFirstMove,
                    isResetGame ? -1 : gameId,
                    isResetGame ? string.Empty : _runningGames[gameId].SecondGamerName);
                _runningGames[gameId].SecondClient.ReceiveGameData(!isFirstMove,
                    isResetGame ? -1 : gameId,
                    isResetGame ? string.Empty : _runningGames[gameId].FirstGamerName);
            });
        }
        public void SendNewMove(byte winningByte, string moveData, int gameId)
        {
            _runningGames[gameId].GetOpponent(OperationContext.Current.GetCallbackChannel<IGameClient>()).
                ReceiveOpponentMove(winningByte, moveData);
        }
        public void StopGame()
        {
            var currentPlayer = OperationContext.Current.GetCallbackChannel<IGameClient>();
            currentPlayer.ReceiveServerMessage(_message_DisconnectFromServer);
            if (_loggedGamers.ContainsKey(currentPlayer))
            {
                //SendToLog($"\r\n{_loggedGamers[currentPlayer]} deleted from list");
                SendToLog(string.Format("\r\n{_loggedGamers[currentPlayer]} deleted from list"));

                _loggedGamers.Clear();
            }
            else
            {
                var currentGameSession = _runningGames.Where(x => x!= null).First(x => x.FirstClient == currentPlayer || x.SecondClient == currentPlayer);
                //SendToLog($"\r\n{_runningGames[currentGameSession.GameId].GetPlayerName(currentPlayer)} has left the game");
                SendToLog(string.Format("\r\n{0} has left the game", _runningGames[currentGameSession.GameId].GetPlayerName(currentPlayer)));
                //SendToLog($"\r\nGame {currentGameSession.GameId}, {_runningGames[currentGameSession.GameId].FirstGamerName} vs {_runningGames[currentGameSession.GameId].SecondGamerName} finished");
                SendToLog(string.Format("\r\nGame {0}, {1} vs {2} finished", 
                    currentGameSession.GameId, _runningGames[currentGameSession.GameId].FirstGamerName, _runningGames[currentGameSession.GameId].SecondGamerName));
                var opponent = _runningGames[currentGameSession.GameId].GetOpponent(currentPlayer);
                //SendToLog($"\r\n{currentGameSession.GetPlayerName(opponent)} moved to list");
                SendToLog(string.Format("\r\n{0} moved to list", currentGameSession.GetPlayerName(opponent)));
                opponent.StopGame(_message_WaitForOthers, false);
                _loggedGamers[opponent] = currentGameSession.GetPlayerName(opponent);
                _runningGames[currentGameSession.GameId] = null;
                _gameCount--;
            }
            GC.Collect();
            if (_loggedGamers.Count == 2)
                StartNewGame();
        }
        public void ResetGame()
        {
            var currentPlayer = OperationContext.Current.GetCallbackChannel<IGameClient>();
            var currentGameSession = _runningGames.First(x => x.FirstClient == currentPlayer || x.SecondClient == currentPlayer);
            SendDataToOpponents(currentGameSession.GameId);
        }
        int SetNewGameId()
        {
            for (int i = 0; i < _runningGames.Length; ++i)
                if (_runningGames[i] == null)
                    return i;
            return -1;
        }
        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                SendServerMessage();
        }
        private void SendServerMessage()
        {
            if (textBox.Text != "")
            {
                //var message = $"admin: {textBox.Text}";
                var message = string.Format("admin: {0}", textBox.Text);
                Parallel.ForEach(_runningGames.Where(x => x != null), x =>
                {
                    x.FirstClient.ReceiveServerMessage(message, true);
                    x.SecondClient.ReceiveServerMessage(message, true);
                });
                if (_loggedGamers.Count > 0)
                    _loggedGamers.Keys.ToArray()[0].ReceiveServerMessage(message, true);
                //SendToLog($"\r\n{message}");
                SendToLog(string.Format("\r\n{0}", message));
                textBox.Text = string.Empty;
            }
        }
        private void SendButton_Click(object sender, EventArgs e)
        {
            SendServerMessage();
        }
    }
}