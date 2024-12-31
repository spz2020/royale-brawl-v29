using Supercell.Laser.Logic.Home.Structures;
using Supercell.Laser.Titan.DataStream;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Supercell.Laser.Logic.Home.Items
{
    public class Notification
    {
        public int Id { get; set; }
        public int Index { get; set; }
        public bool IsViewed { get; set; }
        public int TimePassed { get; set; }
        public string MessageEntry { get; set; }
        public string PrimaryMessageEntry { get; set; }
        public string SecondaryMessageEntry { get; set; }
        public string ButtonMessageEntry { get; set; }
        public string FileLocation { get; set; }
        public string FileSha { get; set; }
        public string ExtLint { get; set; }
        public string Sender { get; set; }
        public int skin { get; set; }
        public List<int> HeroesIds { get; set; }
        public List<int> HeroesTrophies { get; set; }
        public List<int> HeroesTrophiesReseted { get; set; }
        public List<int> StarpointsAwarded { get; set; }
        public int DonationCount;
        public void Encode(ByteStream stream)
        {
            stream.WriteVInt(Id);
            stream.WriteInt(Index);
            stream.WriteBoolean(IsViewed);
            long timestamp = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
            int timestamps = Convert.ToInt32(timestamp % int.MaxValue);

            stream.WriteInt(timestamps - TimePassed);

            stream.WriteString(MessageEntry);
            if (Id == 82)
            {
                stream.WriteStringReference(Sender);
                stream.WriteVInt(0);
                stream.WriteVInt(28000000); // pfp?
                stream.WriteVInt(43000000); // colorname
                stream.WriteVInt(43000000); // colorname2?
            }
            if (Id == 83)
            {
                stream.WriteInt(0);
                stream.WriteStringReference(PrimaryMessageEntry);
                stream.WriteInt(0);
                stream.WriteStringReference(SecondaryMessageEntry);
                stream.WriteInt(0);
                stream.WriteStringReference(ButtonMessageEntry);
                stream.WriteStringReference(FileLocation);
                stream.WriteStringReference(FileSha);
                stream.WriteStringReference(ExtLint);
            }
            if (Id == 79)
            {
                stream.WriteVInt(HeroesIds.Count);
                for (int i = 0; i < HeroesIds.Count; i++)
                {
                    stream.WriteVInt(HeroesIds[i]);
                    stream.WriteVInt(HeroesTrophies[i]);
                    stream.WriteVInt(HeroesTrophiesReseted[i]);
                    stream.WriteVInt(StarpointsAwarded[i]);
                }

            }
            else
            {
                stream.WriteVInt(29000000 + skin); // DisplayData???
            }
            if (Id == 89)
            {
                stream.WriteVInt(DonationCount);
            }
        }
    }
}
