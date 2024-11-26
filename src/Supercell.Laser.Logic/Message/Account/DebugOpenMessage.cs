namespace Supercell.Laser.Logic.Message.Account
{
    public class DebugOpenMessage : GameMessage
    {
        public override int GetMessageType()
        {
            return 20500;
        }

        public override int GetServiceNodeType()
        {
            return 1;
        }
    }
}
