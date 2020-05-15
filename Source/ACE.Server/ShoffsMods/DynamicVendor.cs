using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ACE.Server.ShoffsMods
{
    public class DynamicVendor
    {
        public const ushort maxStackSize = 500;
        private const uint maxValue = 5000000;
        public double priceVarianceFactor { get; set; } = 0.025;
        private uint Wcid;
        private Dictionary<uint, List<Transaction>> pendingTransactions = new Dictionary<uint, List<Transaction>>();

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public DynamicVendor(uint wcid)
        {
            Wcid = wcid;
            ItemValues = new List<GenericItemValue>()
            {
                /* SALVAGE */
                new GenericItemValue(20980),
                new GenericItemValue(20981),
                new GenericItemValue(20982),
                new GenericItemValue(20983),
                new GenericItemValue(20984),
                new GenericItemValue(20985),
                new GenericItemValue(20986),
                new GenericItemValue(20987),
                new GenericItemValue(20988),
                new GenericItemValue(20989),
                new GenericItemValue(20990),
                new GenericItemValue(20991),
                new GenericItemValue(20992),
                new GenericItemValue(20993),
                new GenericItemValue(20994),
                new GenericItemValue(20995),
                new GenericItemValue(21034),
                new GenericItemValue(21035),
                new GenericItemValue(21036),
                new GenericItemValue(21037),
                new GenericItemValue(21038),
                new GenericItemValue(21039),
                new GenericItemValue(21040),
                new GenericItemValue(21041),
                new GenericItemValue(21042),
                new GenericItemValue(21043),
                new GenericItemValue(21044),
                new GenericItemValue(21045),
                new GenericItemValue(21046),
                new GenericItemValue(21047),
                new GenericItemValue(21048),
                new GenericItemValue(21049),
                new GenericItemValue(21050),
                new GenericItemValue(21051),
                new GenericItemValue(21052),
                new GenericItemValue(21053),
                new GenericItemValue(21054),
                new GenericItemValue(21055),
                new GenericItemValue(21056),
                new GenericItemValue(21057),
                new GenericItemValue(21058),
                new GenericItemValue(21059),
                new GenericItemValue(21060),
                new GenericItemValue(21061),
                new GenericItemValue(21062),
                new GenericItemValue(21063),
                new GenericItemValue(21064),
                new GenericItemValue(21065),
                new GenericItemValue(21066),
                new GenericItemValue(21067),
                new GenericItemValue(21068),
                new GenericItemValue(21069),
                new GenericItemValue(21070),
                new GenericItemValue(21071),
                new GenericItemValue(21072),
                new GenericItemValue(21073),
                new GenericItemValue(21074),
                new GenericItemValue(21075),
                new GenericItemValue(21076),
                new GenericItemValue(21077),
                new GenericItemValue(21078),
                new GenericItemValue(21079),
                new GenericItemValue(21080),
                new GenericItemValue(21081),
                new GenericItemValue(21082),
                new GenericItemValue(21083),
                new GenericItemValue(21084),
                new GenericItemValue(21085),
                new GenericItemValue(21086),
                new GenericItemValue(21087),
                new GenericItemValue(21088),
                new GenericItemValue(21089),
                /* PLANTS */
                new GenericItemValue(8037),
                new GenericItemValue(8039),
                new GenericItemValue(8041),
                new GenericItemValue(8644),
                new GenericItemValue(8646),
                new GenericItemValue(8648),
                new GenericItemValue(11553),
                new GenericItemValue(11554),
                new GenericItemValue(11555),
                /* Level 8 Scrolls */
                new GenericItemValue(37992),
                new GenericItemValue(37993),
                new GenericItemValue(37994),
                new GenericItemValue(37995),
                new GenericItemValue(37998),
                new GenericItemValue(37999),
                new GenericItemValue(38000),
                new GenericItemValue(38001),
                new GenericItemValue(38002),
                new GenericItemValue(38006),
                new GenericItemValue(38007),
                new GenericItemValue(37642),
                new GenericItemValue(37640),
                new GenericItemValue(37641),
                new GenericItemValue(37648),
                new GenericItemValue(37644),
                new GenericItemValue(37645),
                new GenericItemValue(46839),
                new GenericItemValue(37665),
                new GenericItemValue(46840),
                new GenericItemValue(37711),
                new GenericItemValue(37809),
                new GenericItemValue(37811),
                new GenericItemValue(37819),
                new GenericItemValue(37818),
                new GenericItemValue(37943),
                new GenericItemValue(37942),
                new GenericItemValue(37961),
                new GenericItemValue(37649),
                new GenericItemValue(37655),
                new GenericItemValue(37656),
                new GenericItemValue(37657),
                new GenericItemValue(37658),
                new GenericItemValue(37661),
                new GenericItemValue(37662),
                new GenericItemValue(37663),
                new GenericItemValue(37666),
                new GenericItemValue(37667),
                new GenericItemValue(37670),
                new GenericItemValue(37672),
                new GenericItemValue(37673),
                new GenericItemValue(37674),
                new GenericItemValue(37675),
                new GenericItemValue(37679),
                new GenericItemValue(37684),
                new GenericItemValue(37685),
                new GenericItemValue(37688),
                new GenericItemValue(37689),
                new GenericItemValue(37693),
                new GenericItemValue(43285),
                new GenericItemValue(43293),
                new GenericItemValue(38757),
                new GenericItemValue(38758),
                new GenericItemValue(37709),
                new GenericItemValue(37710),
                new GenericItemValue(37714),
                new GenericItemValue(43327),
                new GenericItemValue(45243),
                new GenericItemValue(45259),
                new GenericItemValue(37735),
                new GenericItemValue(37736),
                new GenericItemValue(37737),
                new GenericItemValue(45267),
                new GenericItemValue(45283),
                new GenericItemValue(37740),
                new GenericItemValue(37741),
                new GenericItemValue(37742),
                new GenericItemValue(37745),
                new GenericItemValue(37747),
                new GenericItemValue(37749),
                new GenericItemValue(37750),
                new GenericItemValue(43328),
                new GenericItemValue(37753),
                new GenericItemValue(37754),
                new GenericItemValue(37755),
                new GenericItemValue(37757),
                new GenericItemValue(37774),
                new GenericItemValue(37775),
                new GenericItemValue(37776),
                new GenericItemValue(37777),
                new GenericItemValue(37778),
                new GenericItemValue(37779),
                new GenericItemValue(37780),
                new GenericItemValue(37782),
                new GenericItemValue(37783),
                new GenericItemValue(37787),
                new GenericItemValue(37788),
                new GenericItemValue(37789),
                new GenericItemValue(37790),
                new GenericItemValue(37791),
                new GenericItemValue(37792),
                new GenericItemValue(37794),
                new GenericItemValue(37795),
                new GenericItemValue(37796),
                new GenericItemValue(37797),
                new GenericItemValue(37798),
                new GenericItemValue(37799),
                new GenericItemValue(37800),
                new GenericItemValue(37801),
                new GenericItemValue(37802),
                new GenericItemValue(37804),
                new GenericItemValue(37805),
                new GenericItemValue(37806),
                new GenericItemValue(37807),
                new GenericItemValue(37814),
                new GenericItemValue(37815),
                new GenericItemValue(37820),
                new GenericItemValue(37821),
                new GenericItemValue(37822),
                new GenericItemValue(37828),
                new GenericItemValue(37832),
                new GenericItemValue(37833),
                new GenericItemValue(37835),
                new GenericItemValue(37836),
                new GenericItemValue(37839),
                new GenericItemValue(38761),
                new GenericItemValue(37843),
                new GenericItemValue(37844),
                new GenericItemValue(37845),
                new GenericItemValue(37848),
                new GenericItemValue(37852),
                new GenericItemValue(37853),
                new GenericItemValue(37865),
                new GenericItemValue(37867),
                new GenericItemValue(37866),
                new GenericItemValue(37868),
                new GenericItemValue(37854),
                new GenericItemValue(37855),
                new GenericItemValue(37856),
                new GenericItemValue(37857),
                new GenericItemValue(37859),
                new GenericItemValue(37861),
                new GenericItemValue(37862),
                new GenericItemValue(37863),
                new GenericItemValue(37864),
                new GenericItemValue(37872),
                new GenericItemValue(37873),
                new GenericItemValue(37875),
                new GenericItemValue(37880),
                new GenericItemValue(37879),
                new GenericItemValue(38762),
                new GenericItemValue(37883),
                new GenericItemValue(37886),
                new GenericItemValue(38763),
                new GenericItemValue(37890),
                new GenericItemValue(37891),
                new GenericItemValue(37894),
                new GenericItemValue(37897),
                new GenericItemValue(37898),
                new GenericItemValue(37902),
                new GenericItemValue(37903),
                new GenericItemValue(43301),
                new GenericItemValue(44233),
                new GenericItemValue(43309),
                new GenericItemValue(43317),
                new GenericItemValue(37720),
                new GenericItemValue(37721),
                new GenericItemValue(37722),
                new GenericItemValue(37723),
                new GenericItemValue(37724),
                new GenericItemValue(37725),
                new GenericItemValue(37726),
                new GenericItemValue(37914),
                new GenericItemValue(37915),
                new GenericItemValue(38764),
                new GenericItemValue(39093),
                new GenericItemValue(37922),
                new GenericItemValue(45291),
                new GenericItemValue(45307),
                new GenericItemValue(38759),
                new GenericItemValue(38765),
                new GenericItemValue(37929),
                new GenericItemValue(37930),
                new GenericItemValue(45315),
                new GenericItemValue(45331),
                new GenericItemValue(37932),
                new GenericItemValue(37933),
                new GenericItemValue(37934),
                new GenericItemValue(37935),
                new GenericItemValue(37938),
                new GenericItemValue(45339),
                new GenericItemValue(45355),
                new GenericItemValue(37944),
                new GenericItemValue(37945),
                new GenericItemValue(37958),
                new GenericItemValue(37957),
                new GenericItemValue(49463),
                new GenericItemValue(49477),
                new GenericItemValue(37959),
                new GenericItemValue(37965),
                new GenericItemValue(41295),
                new GenericItemValue(41303),
                new GenericItemValue(43374),
                new GenericItemValue(43378),
                new GenericItemValue(37975),
                new GenericItemValue(37979),
                new GenericItemValue(37980),
                new GenericItemValue(37983),
                new GenericItemValue(43337),
                new GenericItemValue(37984),
                new GenericItemValue(37986),
                new GenericItemValue(37987),
                new GenericItemValue(37988),
                new GenericItemValue(37989),
                new GenericItemValue(37991),
                /* Level 8 Spell Comps */
                new GenericItemValue(37343),
                new GenericItemValue(37344),
                new GenericItemValue(37345),
                new GenericItemValue(37346),
                new GenericItemValue(37347),
                new GenericItemValue(37349),
                new GenericItemValue(37350),
                new GenericItemValue(37342),
                new GenericItemValue(37351),
                new GenericItemValue(43379),
                new GenericItemValue(37352),
                new GenericItemValue(45370),
                new GenericItemValue(45371),
                new GenericItemValue(37300),
                new GenericItemValue(37373),
                new GenericItemValue(37301),
                new GenericItemValue(37302),
                new GenericItemValue(37303),
                new GenericItemValue(37348),
                new GenericItemValue(37304),
                new GenericItemValue(37305),
                new GenericItemValue(37369),
                new GenericItemValue(37309),
                new GenericItemValue(37310),
                new GenericItemValue(41746),
                new GenericItemValue(37311),
                new GenericItemValue(37312),
                new GenericItemValue(37313),
                new GenericItemValue(37339),
                new GenericItemValue(37366),
                new GenericItemValue(37367),
                new GenericItemValue(37368),
                new GenericItemValue(37370),
                new GenericItemValue(37314),
                new GenericItemValue(37315),
                new GenericItemValue(37316),
                new GenericItemValue(37317),
                new GenericItemValue(38760),
                new GenericItemValue(37318),
                new GenericItemValue(37319),
                new GenericItemValue(37321),
                new GenericItemValue(37323),
                new GenericItemValue(37324),
                new GenericItemValue(37338),
                new GenericItemValue(37371),
                new GenericItemValue(37372),
                new GenericItemValue(37325),
                new GenericItemValue(43387),
                new GenericItemValue(37326),
                new GenericItemValue(37327),
                new GenericItemValue(37328),
                new GenericItemValue(45372),
                new GenericItemValue(37307),
                new GenericItemValue(37329),
                new GenericItemValue(37330),
                new GenericItemValue(37331),
                new GenericItemValue(45373),
                new GenericItemValue(37332),
                new GenericItemValue(45374),
                new GenericItemValue(37333),
                new GenericItemValue(37336),
                new GenericItemValue(37337),
                new GenericItemValue(49455),
                new GenericItemValue(41747),
                new GenericItemValue(43380),
                new GenericItemValue(37340),
                new GenericItemValue(37341),
                new GenericItemValue(37360),
                new GenericItemValue(37361),
                new GenericItemValue(37353),
                new GenericItemValue(37354),
                new GenericItemValue(37355),
                new GenericItemValue(37357),
                new GenericItemValue(37358),
                new GenericItemValue(37365),
                new GenericItemValue(37362),
                new GenericItemValue(37363),
                new GenericItemValue(37364),
                /* Arrowheads */
                new GenericItemValue(9360),
                new GenericItemValue(21999),
                new GenericItemValue(15421),
                new GenericItemValue(15422),
                new GenericItemValue(15420),
                new GenericItemValue(15423),
                new GenericItemValue(22000),
                new GenericItemValue(15426),
                new GenericItemValue(15427),
                new GenericItemValue(15428),
                new GenericItemValue(15425),
                new GenericItemValue(9365),
                new GenericItemValue(9367),
                new GenericItemValue(9369),
                new GenericItemValue(9370),
                new GenericItemValue(9368),
                new GenericItemValue(9371),
                new GenericItemValue(9374),
                new GenericItemValue(9375),
                new GenericItemValue(9376),
                new GenericItemValue(9373),
                new GenericItemValue(9364),
            };
        }

        public DynamicVendor() { }

        public List<GenericItemValue> ItemValues;

        public void CommitTransactions(uint playerGuid, uint vendorWcid)
        {
            if (pendingTransactions.TryGetValue(playerGuid, out var trx))
            {
                foreach (var transaction in trx)
                {
                    var itemIndex = ItemValues.FindIndex(x => x.Wcid == transaction.ItemWcid);
                    if (itemIndex >= 0)
                    {
                        TryChangeValue(transaction.ItemWcid, GetNewValue(ItemValues[itemIndex].PyrealValue, transaction.Type));
                    }
                }
                pendingTransactions.Remove(playerGuid);
            }
            else
            {
                throw new ArgumentException(nameof(playerGuid));
            }
            Save(vendorWcid);
        }

        public void AddTransactions(List<Transaction> trx, uint playerGuid)
        {
            if (pendingTransactions.ContainsKey(playerGuid))
            {
                pendingTransactions.Remove(playerGuid);
            }
            pendingTransactions.Add(playerGuid, trx);
        }

        public uint GetNewValue(uint curValue, Transaction.TransactionType transactionType)
        {
            double variance = transactionType == Transaction.TransactionType.Buy ? priceVarianceFactor : -priceVarianceFactor;
            int newValue = (int)(curValue + (uint)Math.Round(curValue * variance));
            newValue = newValue < 1 ? 1 : newValue;
            newValue = newValue > (int)maxValue ? (int)maxValue : newValue;
            return (uint)newValue;
        }

        private void Save(uint wcid)
        {
            var serializedObject = Newtonsoft.Json.JsonConvert.SerializeObject(this);
            if (wcid != 21747013) log.Warn($"Wcid = {wcid}");
            File.WriteAllText($"{wcid}.json", serializedObject);
        }

        private bool TryChangeValue(uint wcid, uint newPyrealValue)
        {
            var itemIndex = ItemValues.FindIndex(x => x.Wcid == wcid);
            if (itemIndex >= 0)
            {
                var newValue = newPyrealValue > maxValue ? maxValue : newPyrealValue;
                ItemValues[itemIndex].PyrealValue = newValue;
                return true;
            }
            else
                return false;
        }

        public class GenericItemValue
        {
            public uint PyrealValue { get; set; }
            public uint Wcid { get; set; }

            public GenericItemValue(uint wcid)
            {
                Wcid = wcid;
                PyrealValue = 500000;
            }

            public GenericItemValue() { }
        }

        public class Transaction
        {
            public DateTime Timestamp { get; private set; } = DateTime.Now;
            public uint SalePrice { get; private set; }
            public TransactionType Type { get; private set; }
            public uint ItemWcid { get; set; }

            public Transaction(uint wcid, uint salePrice, TransactionType transactionType)
            {
                ItemWcid = wcid;
                SalePrice = salePrice;
                Type = transactionType;
            }

            public Transaction() { }

            public enum TransactionType
            {
                Buy,
                Sell
            }
        }
    }
}
