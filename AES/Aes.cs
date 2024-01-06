using System.Numerics;

public class Aes
{
    static List<byte[,]> keys = new List<byte[,]>();

    private static void Main(string[] args)
    {
        Vector3 vector = new Vector3(1.0f, 2.0f, 3.0f);

        Console.WriteLine(vector);

        byte[,] byteArray = Vector3ToBytes(vector);

        vector = BytesToVector3(byteArray);

        //byte[,] inputData = new byte[,]
        //{
        //    { 0x32, 0x88, 0x31, 0xe0 },
        //    { 0x43, 0x5a, 0x31, 0x37 },
        //    { 0xf6, 0x30, 0x98, 0x07 },
        //    { 0xa8, 0x8d, 0xa2, 0x34 },
        //};

        var key = new byte[,]
        {
            { 0x2b, 0x28, 0xab, 0x09 },
            { 0x7e, 0xae, 0xf7, 0xcf },
            { 0x15, 0xd2, 0x15, 0x4f },
            { 0x16, 0xa6, 0x88, 0x3c },
        };

        keys = KeySchedule(key);
        Print(byteArray, "");
        var e = E(byteArray);
        vector = BytesToVector3(e);
        Console.WriteLine(vector);
        Print(e, "");
        e = D(e);
        Print(e, "");

        vector = BytesToVector3(e);

        Console.WriteLine(vector);
    }

    static byte[,] Vector3ToBytes(Vector3 vector)
    {
        byte[] xBytes = BitConverter.GetBytes(vector.X);
        byte[] yBytes = BitConverter.GetBytes(vector.Y);
        byte[] zBytes = BitConverter.GetBytes(vector.Z);

        byte[,] result = new byte[4, 4]; // Создание двумерного массива для хранения байтов

        // Заполнение двумерного массива
        result[0, 0] = xBytes[0];
        result[0, 1] = xBytes[1];
        result[0, 2] = xBytes[2];
        result[0, 3] = xBytes[3];

        result[1, 0] = yBytes[0];
        result[1, 1] = yBytes[1];
        result[1, 2] = yBytes[2];
        result[1, 3] = yBytes[3];

        result[2, 0] = zBytes[0];
        result[2, 1] = zBytes[1];
        result[2, 2] = zBytes[2];
        result[2, 3] = zBytes[3];

        result[3, 0] = zBytes[0];
        result[3, 1] = zBytes[1];
        result[3, 2] = zBytes[2];
        result[3, 3] = zBytes[3];

        return result;
    }

    static Vector3 BytesToVector3(byte[,] bytesArray)
    {
        byte[] xBytes = new byte[4];
        byte[] yBytes = new byte[4];
        byte[] zBytes = new byte[4];

        // Extracting bytes from the byte array
        xBytes[0] = bytesArray[0, 0];
        xBytes[1] = bytesArray[0, 1];
        xBytes[2] = bytesArray[0, 2];
        xBytes[3] = bytesArray[0, 3];

        yBytes[0] = bytesArray[1, 0];
        yBytes[1] = bytesArray[1, 1];
        yBytes[2] = bytesArray[1, 2];
        yBytes[3] = bytesArray[1, 3];

        zBytes[0] = bytesArray[2, 0];
        zBytes[1] = bytesArray[2, 1];
        zBytes[2] = bytesArray[2, 2];
        zBytes[3] = bytesArray[2, 3];

        float x = BitConverter.ToSingle(xBytes, 0);
        float y = BitConverter.ToSingle(yBytes, 0);
        float z = BitConverter.ToSingle(zBytes, 0);

        return new Vector3(x, y, z);
    }

    static Vector3 BytesToVector3(byte[] byteArray)
    {
        if (byteArray.Length != sizeof(float) * 3)
        {
            throw new ArgumentException("Invalid byte array length for Vector3");
        }

        // Извлечение байтов для каждой компоненты Vector3
        float x = BitConverter.ToSingle(byteArray, 0);
        float y = BitConverter.ToSingle(byteArray, sizeof(float));
        float z = BitConverter.ToSingle(byteArray, sizeof(float) * 2);

        return new Vector3(x, y, z);
    }

    private static byte[,] E(byte[,] input)
    {
        var data = AddRoundKey(input, RoundKey(0));
        for (int i = 1; i < 10; i++)
        {
            data = SubByte(data);
            data = ShiftRows(data);
            data = MixColumns(data);
            data = AddRoundKey(data, RoundKey(i));
        }
        data = SubByte(data);
        data = ShiftRows(data);
        data = AddRoundKey(data, RoundKey(10));
        return data;
    }

    private static byte[,] D(byte[,] input)
    {
        var data = AddRoundKey(input, RoundKey(10));

        for (int i = 9; i > 0; i--)
        {
            data = InvShiftRows(data);
            data = InvSubByte(data);
            data = AddRoundKey(data, RoundKey(i));
            data = InvMixColumns(data);
        }

        data = InvShiftRows(data);
        data = InvSubByte(data);
        data = AddRoundKey(data, RoundKey(0));

        return data;
    }

    private static byte[,] InvSubByte(byte[,] bytes)
    {
        var newbytes = new byte[bytes.GetLength(0), bytes.GetLength(1)];
        for (int i = 0; i < bytes.GetLength(0); i++)
        {
            for (int j = 0; j < bytes.GetLength(1); j++)
            {
                string hexValue = bytes[i, j].ToString("X2");

                if (hexValue.Length == 1)
                {
                    hexValue = "0" + hexValue;
                }

                int rowIndex = GetIndex(hexValue[0]);
                int colIndex = GetIndex(hexValue[1]);

                newbytes[i, j] = InvSBox(rowIndex, colIndex);
            }
        }
        return newbytes;
    }

    private static byte[,] InvShiftRows(byte[,] bytes)
    {
        var tmp = new byte[4];
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                tmp[(j + i) % 4] = bytes[i, j];
            }
            for (int j = 0; j < 4; j++)
            {
                bytes[i, j] = tmp[j];
            }
        }

        return bytes;
    }

    private static List<byte[,]> KeySchedule(byte[,] key)
    {
        List<byte[,]> roundKey = new List<byte[,]>() { key };

        for (int i = 1; i <= 10; i++)
        {
            //Print(roundKey[i - 1], (i - 1).ToString());
            byte[,] tmpkey = new byte[key.GetLength(0), key.GetLength(1)];
            byte[,] tmpkey1 = new byte[1, key.GetLength(0)];
            byte[,] tmpkey2 = new byte[1, key.GetLength(0)];
            roundKey.Add(new byte[4, 4]);
            for (int j = 0; j < 4; j++)
            {
                if (j == 0)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        tmpkey1[0, k] = (roundKey[i - 1])[(k + 1) % 4, 3];
                    }

                    tmpkey1 = SubByte(tmpkey1);

                    for (int k = 0; k < 4; k++)
                    {
                        tmpkey2[0, k] = (roundKey[i - 1])[k, 0];
                    }

                    var rcon = Rcon(i - 1);

                    for (int k = 0; k < 4; k++)
                    {
                        tmpkey[k, j] = (byte)(tmpkey2[0, k] ^ tmpkey1[0, k] ^ rcon[k]);
                    }
                }
                else
                {
                    if (i == 2 && j == 2)
                    { }

                    tmpkey[0, j] = (byte)(((roundKey[i - 1])[0, j]) ^ (tmpkey[0, j - 1]));
                    tmpkey[1, j] = (byte)(((roundKey[i - 1])[1, j]) ^ (tmpkey[1, j - 1]));
                    tmpkey[2, j] = (byte)(((roundKey[i - 1])[2, j]) ^ (tmpkey[2, j - 1]));
                    tmpkey[3, j] = (byte)(((roundKey[i - 1])[3, j]) ^ (tmpkey[3, j - 1]));
                }
            }
            roundKey[roundKey.Count - 1] = tmpkey;
        }
        return roundKey;
    }

    private static byte[,] RoundKey(int index)
    {
        return keys[index];
        //List<byte[,]> roundKey = new List<byte[,]>()
        //{
        //    new byte[,]
        //    {
        //        { 0x2b, 0x28, 0xab, 0x09 },
        //        { 0x7e, 0xae, 0xf7, 0xcf },
        //        { 0x15, 0xd2, 0x15, 0x4f },
        //        { 0x16, 0xa6, 0x88, 0x3c },
        //    },
        //    new byte[,]
        //    {
        //        { 0xa0, 0x88, 0x23, 0x2a },
        //        { 0xfa, 0x54, 0xa3, 0x6c },
        //        { 0xfe, 0x2c, 0x39, 0x76 },
        //        { 0x17, 0xb1, 0x39, 0x05 },
        //    },
        //    new byte[,]
        //    {
        //        { 0xf2, 0x7a, 0x59, 0x73 },
        //        { 0xc2, 0x96, 0x35, 0x59 },
        //        { 0x95, 0xb9, 0x80, 0xf6 },
        //        { 0xf2, 0x43, 0x7a, 0x7f },
        //    },
        //    new byte[,]
        //    {
        //        { 0x3d, 0x47, 0x1e, 0x6d },
        //        { 0x80, 0x16, 0x23, 0x7a },
        //        { 0x47, 0xfe, 0x7e, 0x88 },
        //        { 0x7d, 0x3e, 0x44, 0x3b },
        //    },
        //    new byte[,]
        //    {
        //        { 0xef, 0xa8, 0xb6, 0xdb },
        //        { 0x44, 0x52, 0x71, 0x0b },
        //        { 0xa5, 0x5b, 0x25, 0xad },
        //        { 0x41, 0x7f, 0x3b, 0x00 },
        //    },
        //    new byte[,]
        //    {
        //        { 0xd4, 0x7c, 0xca, 0x11 },
        //        { 0xd1, 0x83, 0xf2, 0xf9 },
        //        { 0xc6, 0x9d, 0xb8, 0x15 },
        //        { 0xf8, 0x87, 0xbc, 0xbc },
        //    },
        //    new byte[,]
        //    {
        //        { 0x6d, 0x11, 0xdb, 0xca },
        //        { 0x88, 0x0b, 0xf9, 0x00 },
        //        { 0xa3, 0x3e, 0x86, 0x93 },
        //        { 0x7a, 0xfd, 0x41, 0xfd },
        //    },
        //    new byte[,]
        //    {
        //        { 0x4e, 0x5f, 0x84, 0x4e },
        //        { 0x54, 0x5f, 0xa6, 0xa6 },
        //        { 0xf7, 0xc9, 0x4f, 0xdc },
        //        { 0x0e, 0xf3, 0xb2, 0x4f },
        //    },
        //    new byte[,]
        //    {
        //        { 0xea, 0xb5, 0x31, 0x7f },
        //        { 0xd2, 0x8d, 0x2b, 0x8d },
        //        { 0x73, 0xba, 0xf5, 0x29 },
        //        { 0x21, 0xd2, 0x60, 0x2f },
        //    },
        //    new byte[,]
        //    {
        //        { 0xac, 0x19, 0x28, 0x57 },
        //        { 0x77, 0xfa, 0xd1, 0x5c },
        //        { 0x66, 0xdc, 0x29, 0x00 },
        //        { 0xf3, 0x21, 0x41, 0x6e },
        //    },
        //    new byte[,]
        //    {
        //        { 0xd0, 0xc9, 0xe1, 0xb6 },
        //        { 0x14, 0xee, 0x3f, 0x63 },
        //        { 0xf9, 0x25, 0x0c, 0x0c },
        //        { 0xa8, 0x89, 0xc8, 0xa6 },
        //    },
        //};
        //return roundKey[index];
    }

    private static byte[] Rcon(int index)
    {
        List<byte[]> rcon = new List<byte[]>()
        {
            new byte[]{0x01,0x00,0x00,0x00 },
            new byte[]{0x02,0x00,0x00,0x00 },
            new byte[]{0x04,0x00,0x00,0x00 },
            new byte[]{0x08,0x00,0x00,0x00 },
            new byte[]{0x10,0x00,0x00,0x00 },
            new byte[]{0x20,0x00,0x00,0x00 },
            new byte[]{0x40,0x00,0x00,0x00 },
            new byte[]{0x80,0x00,0x00,0x00 },
            new byte[]{0x1b,0x00,0x00,0x00 },
            new byte[]{0x36,0x00,0x00,0x00 },
        };
        return rcon[index];
    }

    private static byte[,] SubByte(byte[,] bytes)
    {
        var newbytes = new byte[bytes.GetLength(0), bytes.GetLength(1)];
        for (int i = 0; i < bytes.GetLength(0); i++)
        {
            for (int j = 0; j < bytes.GetLength(1); j++)
            {
                string hexValue = bytes[i, j].ToString("X2");

                if (hexValue.Length == 1)
                {
                    hexValue = "0" + hexValue;
                }

                int rowIndex = GetIndex(hexValue[0]);
                int colIndex = GetIndex(hexValue[1]);

                newbytes[i, j] = SBox(rowIndex, colIndex);
            }
        }
        return newbytes;
    }

    private static byte[,] ShiftRows(byte[,] bytes)
    {
        var tmp = new byte[4];
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                tmp[j] = bytes[i, (j + i) % 4];
            }
            for (int j = 0; j < 4; j++)
            {
                bytes[i, j] = tmp[j];
            }
        }

        return bytes;
    }

    private static byte[,] AddRoundKey(byte[,] data, byte[,] key)
    {
        byte[,] newdata = new byte[4, 4];
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                newdata[i, j] = (byte)((int)data[i, j] ^ (int)key[i, j]);
            }
        }
        return newdata;
    }

    private static byte SBox(int rowIndex, int colIndex)
    {
        byte[,] sBox = new byte[,]
        {
            { 0x63, 0x7c, 0x77, 0x7b, 0xf2, 0x6b, 0x6f, 0xc5, 0x30, 0x01, 0x67, 0x2b, 0xfe, 0xd7, 0xab, 0x76 },
            { 0xca, 0x82, 0xc9, 0x7d, 0xfa, 0x59, 0x47, 0xf0, 0xad, 0xd4, 0xa2, 0xaf, 0x9c, 0xa4, 0x72, 0xc0 },
            { 0xb7, 0xfd, 0x93, 0x26, 0x36, 0x3f, 0xf7, 0xcc, 0x34, 0xa5, 0xe5, 0xf1, 0x71, 0xd8, 0x31, 0x15 },
            { 0x04, 0xc7, 0x23, 0xc3, 0x18, 0x96, 0x05, 0x9a, 0x07, 0x12, 0x80, 0xe2, 0xeb, 0x27, 0xb2, 0x75 },
            { 0x09, 0x83, 0x2c, 0x1a, 0x1b, 0x6e, 0x5a, 0xa0, 0x52, 0x3b, 0xd6, 0xb3, 0x29, 0xe3, 0x2f, 0x84 },
            { 0x53, 0xd1, 0x00, 0xed, 0x20, 0xfc, 0xb1, 0x5b, 0x6a, 0xcb, 0xbe, 0x39, 0x4a, 0x4c, 0x58, 0xcf },
            { 0xd0, 0xef, 0xaa, 0xfb, 0x43, 0x4d, 0x33, 0x85, 0x45, 0xf9, 0x02, 0x7f, 0x50, 0x3c, 0x9f, 0xa8 },
            { 0x51, 0xa3, 0x40, 0x8f, 0x92, 0x9d, 0x38, 0xf5, 0xbc, 0xb6, 0xda, 0x21, 0x10, 0xff, 0xf3, 0xd2 },
            { 0xcd, 0x0c, 0x13, 0xec, 0x5f, 0x97, 0x44, 0x17, 0xc4, 0xa7, 0x7e, 0x3d, 0x64, 0x5d, 0x19, 0x73 },
            { 0x60, 0x81, 0x4f, 0xdc, 0x22, 0x2a, 0x90, 0x88, 0x46, 0xee, 0xb8, 0x14, 0xde, 0x5e, 0x0b, 0xdb },
            { 0xe0, 0x32, 0x3a, 0x0a, 0x49, 0x06, 0x24, 0x5c, 0xc2, 0xd3, 0xac, 0x62, 0x91, 0x95, 0xe4, 0x79 },
            { 0xe7, 0xc8, 0x37, 0x6d, 0x8d, 0xd5, 0x4e, 0xa9, 0x6c, 0x56, 0xf4, 0xea, 0x65, 0x7a, 0xae, 0x08 },
            { 0xba, 0x78, 0x25, 0x2e, 0x1c, 0xa6, 0xb4, 0xc6, 0xe8, 0xdd, 0x74, 0x1f, 0x4b, 0xbd, 0x8b, 0x8a },
            { 0x70, 0x3e, 0xb5, 0x66, 0x48, 0x03, 0xf6, 0x0e, 0x61, 0x35, 0x57, 0xb9, 0x86, 0xc1, 0x1d, 0x9e },
            { 0xe1, 0xf8, 0x98, 0x11, 0x69, 0xd9, 0x8e, 0x94, 0x9b, 0x1e, 0x87, 0xe9, 0xce, 0x55, 0x28, 0xdf },
            { 0x8c, 0xa1, 0x89, 0x0d, 0xbf, 0xe6, 0x42, 0x68, 0x41, 0x99, 0x2d, 0x0f, 0xb0, 0x54, 0xbb, 0x16 },
        };

        return sBox[rowIndex, colIndex];
    }

    private static byte InvSBox(int rowIndex, int colIndex)
    {
        byte[,] InvsBox = new byte[,]
        {
            { 0x52, 0x09, 0x6A, 0xD5, 0x30, 0x36, 0xA5, 0x38, 0xBF, 0x40, 0xA3, 0x9E, 0x81, 0xF3, 0xD7, 0xFB },
            { 0x7C, 0xE3, 0x39, 0x82, 0x9B, 0x2F, 0xFF, 0x87, 0x34, 0x8E, 0x43, 0x44, 0xC4, 0xDE, 0xE9, 0xCB },
            { 0x54, 0x7B, 0x94, 0x32, 0xA6, 0xC2, 0x23, 0x3D, 0xEE, 0x4C, 0x95, 0x0B, 0x42, 0xFA, 0xC3, 0x4E },
            { 0x08, 0x2E, 0xA1, 0x66, 0x28, 0xD9, 0x24, 0xB2, 0x76, 0x5B, 0xA2, 0x49, 0x6D, 0x8B, 0xD1, 0x25 },
            { 0x72, 0xF8, 0xF6, 0x64, 0x86, 0x68, 0x98, 0x16, 0xD4, 0xA4, 0x5C, 0xCC, 0x5D, 0x65, 0xB6, 0x92 },
            { 0x6C, 0x70, 0x48, 0x50, 0xFD, 0xED, 0xB9, 0xDA, 0x5E, 0x15, 0x46, 0x57, 0xA7, 0x8D, 0x9D, 0x84 },
            { 0x90, 0xD8, 0xAB, 0x00, 0x8C, 0xBC, 0xD3, 0x0A, 0xF7, 0xE4, 0x58, 0x05, 0xB8, 0xB3, 0x45, 0x06 },
            { 0xD0, 0x2C, 0x1E, 0x8F, 0xCA, 0x3F, 0x0F, 0x02, 0xC1, 0xAF, 0xBD, 0x03, 0x01, 0x13, 0x8A, 0x6B },
            { 0x3A, 0x91, 0x11, 0x41, 0x4F, 0x67, 0xDC, 0xEA, 0x97, 0xF2, 0xCF, 0xCE, 0xF0, 0xB4, 0xE6, 0x73 },
            { 0x96, 0xAC, 0x74, 0x22, 0xE7, 0xAD, 0x35, 0x85, 0xE2, 0xF9, 0x37, 0xE8, 0x1C, 0x75, 0xDF, 0x6E },
            { 0x47, 0xF1, 0x1A, 0x71, 0x1D, 0x29, 0xC5, 0x89, 0x6F, 0xB7, 0x62, 0x0E, 0xAA, 0x18, 0xBE, 0x1B },
            { 0xFC, 0x56, 0x3E, 0x4B, 0xC6, 0xD2, 0x79, 0x20, 0x9A, 0xDB, 0xC0, 0xFE, 0x78, 0xCD, 0x5A, 0xF4 },
            { 0x1F, 0xDD, 0xA8, 0x33, 0x88, 0x07, 0xC7, 0x31, 0xB1, 0x12, 0x10, 0x59, 0x27, 0x80, 0xEC, 0x5F },
            { 0x60, 0x51, 0x7F, 0xA9, 0x19, 0xB5, 0x4A, 0x0D, 0x2D, 0xE5, 0x7A, 0x9F, 0x93, 0xC9, 0x9C, 0xEF },
            { 0xA0, 0xE0, 0x3B, 0x4D, 0xAE, 0x2A, 0xF5, 0xB0, 0xC8, 0xEB, 0xBB, 0x3C, 0x83, 0x53, 0x99, 0x61 },
            { 0x17, 0x2B, 0x04, 0x7E, 0xBA, 0x77, 0xD6, 0x26, 0xE1, 0x69, 0x14, 0x63, 0x55, 0x21, 0x0C, 0x7D },
        };

        return InvsBox[rowIndex, colIndex];
    }

    private static int GetIndex(char value)
    {
        if (value >= '0' && value <= '9')
            return value - '0';
        else if (value >= 'A' && value <= 'F')
            return value - 'A' + 10;
        else
            return -1;
    }

    private static byte GMul(byte u, byte v)
    {
        byte p = 0;

        for (int i = 0; i < 8; ++i)
        {
            if ((u & 0x01) != 0)
            {
                p ^= v;
            }

            int flag = (v & 0x80);
            v <<= 1;
            if (flag != 0)
            {
                v ^= 0x1B; /* x^8 + x^4 + x^3 + x + 1 */
            }

            u >>= 1;
        }

        return p;
    }

    private static byte[,] MixColumns(byte[,] state)
    {
        byte[,] tmp = new byte[4, 4];
        byte[,] M = {
            {0x02, 0x03, 0x01, 0x01},
            {0x01, 0x02, 0x03, 0x01},
            {0x01, 0x01, 0x02, 0x03},
            {0x03, 0x01, 0x01, 0x02}
        };

        for (int i = 0; i < 4; ++i)
        {
            for (int j = 0; j < 4; ++j)
            {
                tmp[i, j] = state[i, j];
            }
        }

        for (int i = 0; i < 4; ++i)
        {
            for (int j = 0; j < 4; ++j)
            {
                state[i, j] = (byte)(GMul(M[i, 0], tmp[0, j]) ^ GMul(M[i, 1], tmp[1, j]) ^ GMul(M[i, 2], tmp[2, j]) ^ GMul(M[i, 3], tmp[3, j]));
            }
        }

        return state;
    }

    private static byte[,] InvMixColumns(byte[,] state)
    {
        byte[,] tmp = new byte[4, 4];
        byte[,] M = {
            {0x0E, 0x0B, 0x0D, 0x09},
            {0x09, 0x0E, 0x0B, 0x0D},
            {0x0D, 0x09, 0x0E, 0x0B},
            {0x0B, 0x0D, 0x09, 0x0E}
        };

        for (int i = 0; i < 4; ++i)
        {
            for (int j = 0; j < 4; ++j)
            {
                tmp[i, j] = state[i, j];
            }
        }

        for (int i = 0; i < 4; ++i)
        {
            for (int j = 0; j < 4; ++j)
            {
                state[i, j] = (byte)(GMul(M[i, 0], tmp[0, j]) ^ GMul(M[i, 1], tmp[1, j]) ^ GMul(M[i, 2], tmp[2, j]) ^ GMul(M[i, 3], tmp[3, j]));
            }
        }
        return state;
    }

    public static void Print(byte[,] data, string text)
    {
        Console.WriteLine(text);
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                Console.Write(data[i, j].ToString("x2") + "\t");
            }
            Console.WriteLine();
        }
        Console.WriteLine();
    }
}
