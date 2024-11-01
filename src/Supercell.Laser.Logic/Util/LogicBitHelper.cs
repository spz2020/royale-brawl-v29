namespace Supercell.Laser.Logic.Util
{
    using Supercell.Laser.Titan.Debug;
    using Supercell.Laser.Titan.Math;
    using System.Numerics;

    public static class LogicBitHelper
    {
        public static bool Get(BigInteger value, int index)
        {
            // Здвигаємо біт вправо до потрібної позиції та витягуємо його
            return ((value >> index) & BigInteger.One) == BigInteger.One;
        }

        public static BigInteger Set(BigInteger value, int index, bool data)
        {
            // remove the bit according to the specified index
            value &= ~(BigInteger.One << index);

            // new bit if data == true
            if (data)
                value |= (BigInteger.One << index);

            return value;
        }
    }
}
