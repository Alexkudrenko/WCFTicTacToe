using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameInterfaces;

namespace TicTacToe
{
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant, UseSynchronizationContext = false)]
    public partial class GameForm : Form, IGameClient
    {
        #region Messages
        string _message_YourMove;
        string _message_waitingForMove;
        string _message_UnableToConnect = "Unable connect to server";
        #endregion

        string xImage = "./img/x1.png";
        private string oImage = "./img/o1.png";
        string _data = string.Empty;
        byte _resultByte;
        Image _youImg, _opponentImg;
        List<Panel> _allPanels = new List<Panel>();
        GameLogic _gameLogic = new GameLogic();
        GameData _gameData = new GameData();
        ChannelFactory<IGameServer> _channelFactory;
        IGameServer _server;
        bool isConnected;
        public GameForm()
        {
            InitializeComponent();
            InitGamePanels();
            _channelFactory = new DuplexChannelFactory<IGameServer>(this, "GameClientEP");
            _server = _channelFactory.CreateChannel();
            nameTextBox.Text = "";
        }
        void InitMessages()
        {
            //_message_waitingForMove = $"Waiting for {_gameData.OpponentName} move";
            _message_waitingForMove = string.Format("Waiting for {0} move", _gameData.OpponentName);

            //_message_YourMove = $"Your move, {_gameData.PlayerName}";
            _message_YourMove = string.Format("Your move, {0}!", _gameData.PlayerName);

        }
        void InitGamePanels()
        {
            _allPanels.Add(panel1);
            _allPanels.Add(panel2);
            _allPanels.Add(panel3);
            _allPanels.Add(panel4);
            _allPanels.Add(panel5);
            _allPanels.Add(panel6);
            _allPanels.Add(panel7);
            _allPanels.Add(panel8);
            _allPanels.Add(panel9);
            Parallel.ForEach(_allPanels, x => { x.BackgroundImage = null; x.Enabled = false; });
        }
        void DrawMove(string data, Image img)
        {
            var selectedPanel = _allPanels.First(x => x.Tag.ToString() == data);
            selectedPanel.BackgroundImage = img;
            selectedPanel.Enabled = false;
            Refresh();
        }
        void ResetGame()
        {
            _gameLogic.ResetField();
            Parallel.ForEach(_allPanels, x => { x.BackgroundImage = null;});
        }
        private async void connectButton_Click(object sender, EventArgs e)
        {
            _gameData.PlayerName = nameTextBox.Text;
            try
            {
                connectButton.Enabled = !connectButton.Enabled;
                if (connectButton.Text == "Connect")
                {
                    await Task.Factory.StartNew(() =>
                    {
                        _server = _channelFactory.CreateChannel();
                        _server.ConnectToServer(_gameData.PlayerName);
                        //this.Text = $"Tic Tac Toe, [{nameTextBox.Text}]";
                        this.Text = string.Format("Tic Tac Toe, [{0}]", nameTextBox.Text);
                        isConnected = true;
                    });
                }
                else
                    _server.StopGame();
                nameTextBox.Visible = false;
                connectButton.Text = connectButton.Text == "Connect" ? "Disconnect" : "Connect";
                connectButton.Enabled = !connectButton.Enabled;
            }
            catch (Exception)
            {
                infoLabel.Text = (_message_UnableToConnect);
                connectButton.Enabled = !connectButton.Enabled;
                isConnected = false;
                connectButton.Text = "Connect";
            }
        }
        private void panel_Click(object sender, EventArgs e)
        {
            infoLabel.Text = _message_waitingForMove;
            var selectedPanel = (sender as Panel);
            selectedPanel.Enabled = false;
            selectedPanel.BackgroundImage = _youImg;
            _gameLogic.NewMove(_data = selectedPanel.Tag.ToString(), _gameData.PlayerChar);
            DrawMove(_data, _youImg);
            _resultByte = _gameLogic.CheckWin(_gameData.PlayerChar);
            _server.SendNewMove(_resultByte, _data, _gameData.GameId);
            if (_resultByte != 0) //win
            {
                //infoLabel.Text = _resultByte == 1 ? $"You win, {_gameData.PlayerName}!" : "Draw!";
                infoLabel.Text = _resultByte == 1 ? string.Format("You win, {0}", _gameData.PlayerName) : "Draw!";

                var t1 = new Thread(ResetGameInThread);
                t1.Start();
                return;
            }
            Parallel.ForEach(_allPanels.Where(x => x.BackgroundImage == null), x => { x.Enabled = false; });
        }
        public void ReceiveServerMessage(string message, bool isAdminMessage = false)
        {
            if (isAdminMessage)
            {
                infoLabel.ForeColor = Color.Chocolate;
                var buff = infoLabel.Text;
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(1500);
                    infoLabel.ForeColor = SystemColors.MenuHighlight;
                    infoLabel.Text = buff;
                });
            }
            infoLabel.Text = message;
        }
        public void ReceiveGameData(bool isFirstMove, int gameId, string opponentName)
        {
            ResetGame();
            _gameData.IsFirstMove = isFirstMove;
            if (gameId != -1)
                _gameData.GameId = gameId;
            if (opponentName != "")
                _gameData.OpponentName = opponentName;
            InitMessages();
            Parallel.ForEach(_allPanels, x => { x.Enabled = _gameData.IsFirstMove; });
            infoLabel.Text = _gameData.IsFirstMove ? _message_YourMove : _message_waitingForMove;
            _youImg = _gameData.IsFirstMove ? Image.FromFile(xImage) : Image.FromFile(oImage);
            _opponentImg = !_gameData.IsFirstMove ? Image.FromFile(xImage) : Image.FromFile(oImage);
            _gameData.PlayerChar = (byte)(_gameData.IsFirstMove ? 1 : 2);
            _gameData.OpponentByte = (byte)(_gameData.IsFirstMove ? 2 : 1);
        }
        public void ReceiveOpponentMove(byte winningByte, string moveData)
        {
            _data = moveData;
            _resultByte = winningByte;
            _gameLogic.NewMove(_data, _gameData.OpponentByte);
            DrawMove(_data, _opponentImg);
            if (_resultByte != 0)
            {
                infoLabel.Text = _resultByte == 1 ? "You loose!" : "Draw!";
                //MessageBox.Show(_resultByte == 1 ? "You loose!" : "Draw!");
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(3000);
                    ResetGame();
                });
                //ResetGame();
                return;
            }
            infoLabel.Text = _message_YourMove;
            Parallel.ForEach(_allPanels.Where(x => x.BackgroundImage == null), x => x.Enabled = true);
        }
        private void GameForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if(isConnected)
                    _server.StopGame();
            }
            catch (Exception)
            {   
            }
            
        }
        public void StopGame(string message, bool isServerDown)
        {
            infoLabel.Text = message;
            Parallel.ForEach(_allPanels, (x) => { x.Enabled = false; });
            if (isServerDown)
                connectButton.Text = "Connect";
        }
        void ResetGameInThread()
        {
            Thread.Sleep(3000);
            ResetGame();
            infoLabel.Text = _message_YourMove;
            _server.ResetGame();
        }
        class GameData
        {
            public byte PlayerChar;
            public byte OpponentByte;
            public bool IsFirstMove;
            public string PlayerName;
            public string OpponentName;
            public int GameId;
        }
    }
}