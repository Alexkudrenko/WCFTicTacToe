using System.ServiceModel;

namespace GameInterfaces
{
    [ServiceContract(CallbackContract = typeof(IGameClient))]
    public interface IGameServer
    {
        [OperationContract]
        void ConnectToServer(string name);

        [OperationContract]
        void SendNewMove(byte winningByte, string moveData, int gameId);

        [OperationContract]
        void StopGame();

        [OperationContract]
        void ResetGame();
    }

    public interface IGameClient
    {
        [OperationContract(IsOneWay = true)]
        void ReceiveServerMessage(string message, bool isAdminMessage = false);

        [OperationContract(IsOneWay = true)]
        void ReceiveGameData(bool isFirstMove, int gameId, string opponentName);

        [OperationContract(IsOneWay = true)]
        void ReceiveOpponentMove(byte winningByte, string moveData);

        [OperationContract(IsOneWay = true)]
        void StopGame(string message, bool isServerDown);
    }
}