﻿namespace Supercell.Laser.Logic.Message.Account.Auth
{
    public class AuthenticationFailedMessage : GameMessage
    {
        public int ErrorCode;
        public string FingerprintSha;
        public string Message;
        public string UpdateUrl;
        public string ContentUrl;
        public string Redirect;

        public override void Encode()
        {
            Stream.WriteInt(ErrorCode);
            Stream.WriteString(FingerprintSha);
            Stream.WriteString(Redirect); // Redirect
            Stream.WriteString(ContentUrl); // content url
            Stream.WriteString(UpdateUrl); // update url
            Stream.WriteString(Message);
            Stream.WriteInt(-1);
            Stream.WriteBoolean(false);

            Stream.WriteInt(0);
            Stream.WriteInt(0);
            Stream.WriteInt(0);
            Stream.WriteInt(0);
            Stream.WriteInt(0);
            Stream.WriteInt(0);
            Stream.WriteInt(0);
            Stream.WriteInt(0);
            Stream.WriteInt(0);
        }

        public override int GetMessageType()
        {
            return 20103;
        }

        public override int GetServiceNodeType()
        {
            return 1;
        }
    }
}
