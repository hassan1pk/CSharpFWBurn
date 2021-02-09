using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSharpFWBurn
{
    class Program
    {
        static SerialPort _serialPort = null;
        const long LPC_RAMSTART_LPC11XX = 0x10000000L;
        const long LPC_RAMBASE_LPC11XX = 0x10000300L;


        public class LPCTYPESSTRUCT
        {
            public uint RAMSize;
            public uint FlashSectors;
            public uint MaxCopySize;
            public uint[] SectorTable;
        }

        public class ISP_ENVIRONMENTSTRUCT
        {
            public string StringOscillator;
            public byte[] binaryContent;
            public int nQuestionMarks;
            public uint binaryOffset;
            public uint binaryLength;
        }

        public static ISP_ENVIRONMENTSTRUCT ISP_ENVIRONMENT = new ISP_ENVIRONMENTSTRUCT();
        public static LPCTYPESSTRUCT LPCTypes = new LPCTYPESSTRUCT();

        static uint[] SectorTable_11xx =
        {
             4096,  4096,  4096,  4096,  4096,  4096,  4096,  4096,
             4096,  4096,  4096,  4096,  4096,  4096,  4096,  4096,
             4096,  4096,  4096,  4096,  4096,  4096,  4096,  4096,
             4096,  4096,  4096,  4096,  4096,  4096,  4096,  4096
        };

        static void Main(string[] args)
        {
            ISP_ENVIRONMENT.StringOscillator = "12000";
            ISP_ENVIRONMENT.binaryContent = ReadFirmwareData();
            ISP_ENVIRONMENT.nQuestionMarks = 10;
            ISP_ENVIRONMENT.binaryOffset = 0;
            ISP_ENVIRONMENT.binaryLength = Convert.ToUInt32(ISP_ENVIRONMENT.binaryContent.Length);

            LPCTypes.RAMSize = 8;
            LPCTypes.FlashSectors = 8;
            LPCTypes.MaxCopySize = 4096;
            LPCTypes.SectorTable = SectorTable_11xx;


            _serialPort = new SerialPort("COM5", 9600, Parity.None, 8, StopBits.One);
            _serialPort.Handshake = Handshake.None;
            _serialPort.ReadTimeout = 1000;
            _serialPort.Open();

            File.WriteAllText("MyLogFile.txt", "");

            NxpDownload();

            _serialPort.Close();

            Console.Write("Press any key to exit...");
            Console.ReadKey();

        }

        public static void ReceiveComPort(ref string Answer, int timeoutInMilliseconds, bool verify, bool discardRemainingData = false)
        {
            Answer = "";


            try
            {
                _serialPort.ReadTimeout = timeoutInMilliseconds;
                
                Answer = _serialPort.ReadLine().Trim() + "\r\n";
                if (verify)
                {
                    Answer += _serialPort.ReadLine().Trim() + "\r\n";
                }


                if (discardRemainingData == true)
                {
                    Thread.Sleep(timeoutInMilliseconds);
                    _serialPort.ReadExisting();
                }

            }
            catch (TimeoutException) { }


        }

        public static void SendComPort(string data)
        {
            File.AppendAllText("MyLogFile.txt", data+"\r\n");
            _serialPort.Write(data);
        }

        public static byte[] ReadFirmwareData()
        {
            string text = File.ReadAllText("byte_array27_SK.txt");
            string[] strNumbers = text.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            byte[] resultArr = new byte[strNumbers.Length];
            for (int i = 0; i < strNumbers.Length; i++)
            {
                resultArr[i] = Convert.ToByte(strNumbers[i].Trim());
            }
            return resultArr;

        }

        public static void NxpDownload()
        {
            string Answer = "";
            uint Sector=0, SectorStart=0, SectorLength, SectorOffset, SectorChunk=0, CopyLength, block_CRC;
            uint ivt_CRC;          // CRC over interrupt vector table
            int Line;
            uint c, k = 0;
            char[] uuencode_table = new char[64];

            char[] sendbuf0 = new char[128];
            char[] sendbuf1 = new char[128];
            char[] sendbuf2 = new char[128];
            char[] sendbuf3 = new char[128];
            char[] sendbuf4 = new char[128];
            char[] sendbuf5 = new char[128];
            char[] sendbuf6 = new char[128];
            char[] sendbuf7 = new char[128];
            char[] sendbuf8 = new char[128];
            char[] sendbuf9 = new char[128];
            char[] sendbuf10 = new char[128];
            char[] sendbuf11 = new char[128];
            char[] sendbuf12 = new char[128];
            char[] sendbuf13 = new char[128];
            char[] sendbuf14 = new char[128];
            char[] sendbuf15 = new char[128];
            char[] sendbuf16 = new char[128];
            char[] sendbuf17 = new char[128];
            char[] sendbuf18 = new char[128];
            char[] sendbuf19 = new char[128];

            char[][] sendbuf = { sendbuf0,  sendbuf1,  sendbuf2,  sendbuf3,  sendbuf4,
                              sendbuf5,  sendbuf6,  sendbuf7,  sendbuf8,  sendbuf9,
                              sendbuf10, sendbuf11, sendbuf12, sendbuf13, sendbuf14,
                              sendbuf15, sendbuf16, sendbuf17, sendbuf18, sendbuf19 };

            Console.WriteLine("Synchronizing (ESC to abort)");
            ReceiveComPort(ref Answer, 1000, false);
            if (Answer != null && Answer.Contains('|'))
            {
                string fw = Answer.Substring(Answer.IndexOf('|') + 1);
                Console.WriteLine("Current FW: " + fw);
                Console.WriteLine("Putting sensor into ISP Mode");
                SendComPort("ispmode\r");

                for (int i = 0; i < 5; i++)
                {
                    ReceiveComPort(ref Answer, 1000, false);
                    Console.Write(".");
                }
                if (Answer.Length > 0)
                {
                    SendComPort("ispmode\r");

                    for (int i = 0; i < 5; i++)
                    {
                        ReceiveComPort(ref Answer, 1000, false);
                        Console.Write(".");
                    }
                    if (Answer.Length > 0)
                    {
                        Console.WriteLine("ALERT!!! ALERT!!!");
                    }
                }
            }
            else
            {
                Console.WriteLine("Not a mimo sensor -- retrying");
            }

            for (int cycle = 0; cycle < 2; cycle++)
            {
                bool found = false;
                int nQuestionMarks = 0;
                for (nQuestionMarks = 0, found = false; found == false && nQuestionMarks < ISP_ENVIRONMENT.nQuestionMarks; nQuestionMarks++)
                {
                    Console.Write(".");
                    SendComPort("?");
                    ReceiveComPort(ref Answer, 1000, false);
                    if (Answer.Contains("Synchronized"))
                    {
                        found = true;
                    }

                }
                if (found == false)
                {
                    Console.WriteLine(" no answer on '?'");
                    return;
                }

                Console.WriteLine("OK");
                SendComPort("Synchronized\r\n");
                ReceiveComPort(ref Answer, 1000, true);
                if (!Answer.Contains("Synchronized\r\nOK"))
                {
                    Console.WriteLine("No answer on synchronized");
                    return;
                }

                Console.WriteLine("Synchronized 1");
                Console.WriteLine("Setting Oscillator");
                SendComPort(ISP_ENVIRONMENT.StringOscillator + "\r\n");
                ReceiveComPort(ref Answer, 1000, true);
                if (!Answer.Contains(ISP_ENVIRONMENT.StringOscillator + "\r\nOK"))
                {
                    Console.WriteLine("No answer on oscillator command");
                    return;
                }

                Console.WriteLine("Unlock");
                SendComPort("U 23130\r\n");
                ReceiveComPort(ref Answer, 1000, false);
                if (!Answer.Contains("U 23130"))
                {
                    Console.WriteLine("Unlock error");
                    return;
                }

                SendComPort("J\r\n");
                ReceiveComPort(ref Answer, 5000, false, true);

                // Build up uuencode table
                uuencode_table[0] = (char)0x60;           // 0x20 is translated to 0x60 !

                for (int i = 1; i < 64; i++)
                {
                    uuencode_table[i] = (char)(0x20 + i);
                }

                // Patch 0x1C, otherwise it is not running and jumps to boot mode

                ivt_CRC = 0;

                // Clear the vector at 0x1C so it doesn't affect the checksum:
                for (int i = 0; i < 4; i++)
                {
                    ISP_ENVIRONMENT.binaryContent[i + 0x1C] = 0;
                }

                // Calculate a native checksum of the little endian vector table:
                for (int i = 0; i < (4 * 8);)
                {
                    ivt_CRC += (uint)ISP_ENVIRONMENT.binaryContent[i++];
                    ivt_CRC += (uint)(ISP_ENVIRONMENT.binaryContent[i++] << 8);
                    ivt_CRC += (uint)(ISP_ENVIRONMENT.binaryContent[i++]) << 16;
                    ivt_CRC += (uint)(ISP_ENVIRONMENT.binaryContent[i++] << 24);
                }

                /* Negate the result and place in the vector at 0x1C as little endian
	            * again. The resulting vector table should checksum to 0. */
                ivt_CRC = (uint)(0 - ivt_CRC);
                for (int i = 0; i < 4; i++)
                {
                    ISP_ENVIRONMENT.binaryContent[i + 0x1C] = (byte)(sbyte)(ivt_CRC >> (8 * i));
                }

                Console.WriteLine("Position 0x1C patched: ivt_CRC = " + String.Format("#{0:X}", ivt_CRC));

                if ((ISP_ENVIRONMENT.binaryOffset >= LPC_RAMSTART_LPC11XX)
                && (ISP_ENVIRONMENT.binaryOffset + ISP_ENVIRONMENT.binaryLength <= (uint)LPC_RAMSTART_LPC11XX + (LPCTypes.RAMSize * 1024)))
                {
                    LPCTypes.FlashSectors = 1;
                    LPCTypes.MaxCopySize = LPCTypes.RAMSize * 1024 - (uint)(LPC_RAMBASE_LPC11XX - LPC_RAMSTART_LPC11XX);
                    uint[] SectorTable_RAM = { 6500 };
                    LPCTypes.SectorTable = SectorTable_RAM;
                    SectorTable_RAM[0] = LPCTypes.MaxCopySize;
                }

                Console.WriteLine("Will start programming at Sector 1 if possible, and conclude with Sector 0 to ensure that checksum is written last.");
                if (LPCTypes.SectorTable[0] >= ISP_ENVIRONMENT.binaryLength)
                {
                    Sector = 0;
                    SectorStart = 0;
                }
                else
                {
                    SectorStart = LPCTypes.SectorTable[0];
                    Sector = 1;
                }

                SendComPort("P 0 0\r\n");
                ReceiveComPort(ref Answer, 5000, true);
                if(!Answer.Contains("P 0 0\r\n0\r\n"))
                {
                    Console.WriteLine("Wrong Answer on prepare command");
                    return;
                }

                SendComPort("E 0 0\r\n");
                ReceiveComPort(ref Answer, 5000, true);
                if (!Answer.Contains("E 0 0\r\n0\r\n"))
                {
                    Console.WriteLine("Wrong Answer on erase command");
                    return;
                }

                Console.WriteLine("OK ");

                if(cycle == 0)
                {
                    SendComPort("G 0 T\r\n");

                    if ((ISP_ENVIRONMENT.binaryOffset < LPC_RAMSTART_LPC11XX)
                    || (ISP_ENVIRONMENT.binaryOffset >= (uint)LPC_RAMSTART_LPC11XX + (LPCTypes.RAMSize * 1024)))
                    {
                        ReceiveComPort(ref Answer, 5000, true);

                        if(!Answer.Contains("G 0 T\r\n0") && !Answer.Contains("G 0 T\n\0"))
                        {
                            Console.WriteLine("Failed to run the new downloaded code.");
                        }
                    }
                }      


            }

            while (true)
            {
                if (Sector >= LPCTypes.FlashSectors)
                {
                    Console.WriteLine("Program too large; running out of Flash sectors.");
                    return;
                }

                Console.WriteLine("Sector " + Sector + ": ");

                if ((ISP_ENVIRONMENT.binaryOffset < LPC_RAMSTART_LPC11XX)  // Skip Erase when running from RAM
                || (ISP_ENVIRONMENT.binaryOffset >= (uint)LPC_RAMSTART_LPC11XX + (LPCTypes.RAMSize * 1024)))
                {
                    SendComPort("P " + Sector + " " + Sector + "\r\n");
                    ReceiveComPort(ref Answer, 5000, true);
                    if (!Answer.Contains("P " + Sector + " " + Sector + "\r\n0\r\n"))
                    {
                        Console.WriteLine("Wrong answer on Prepare-Command (1) (Sector " + Sector + ")");
                        return;
                    }

                    Console.Write(".");

                    if (Sector != 0) //Sector 0 already erased
                    {
                        SendComPort("E " + Sector + " " + Sector + "\r\n");
                        ReceiveComPort(ref Answer, 5000, true);
                        if (!Answer.Contains("E " + Sector + " " + Sector + "\r\n0\r\n"))
                        {
                            Console.WriteLine("Wrong answer on Erase-Command (1) (Sector " + Sector + ")");
                            return;
                        }
                    }
                }

                SectorLength = LPCTypes.SectorTable[Sector];
                if (SectorLength > ISP_ENVIRONMENT.binaryLength - SectorStart)
                {
                    SectorLength = ISP_ENVIRONMENT.binaryLength - SectorStart;
                }

                for (SectorOffset = 0; SectorOffset < SectorLength; SectorOffset += SectorChunk)
                {
                    // Check if we are to write only 0xFFs - it would be just a waste of time..
                    if (SectorOffset == 0)
                    {
                        for (SectorOffset = 0; SectorOffset < SectorLength; ++SectorOffset)
                        {
                            if (ISP_ENVIRONMENT.binaryContent[SectorStart + SectorOffset] != 0xFF)
                                break;
                        }
                        if (SectorOffset == SectorLength) // all data contents were 0xFFs
                        {
                            Console.WriteLine("Whole sector contents is 0xFFs, skipping programming.");
                            break;
                        }
                        SectorOffset = 0; // re-set otherwise
                    }

                    if (SectorOffset > 0)
                    {
                        // Add a visible marker between segments in a sector
                        Console.WriteLine("|");  /* means: partial segment copied */
                    }

                    SectorChunk = SectorLength - SectorOffset;
                    if (SectorChunk > LPCTypes.MaxCopySize)
                    {
                        SectorChunk = LPCTypes.MaxCopySize;
                    }

                    CopyLength = SectorChunk;

                    if ((CopyLength % (45 * 4)) != 0)
                    {
                        CopyLength += ((45 * 4) - (CopyLength % (45 * 4)));
                    }

                    SendComPort("W " + LPC_RAMBASE_LPC11XX + " " + CopyLength + "\r\n");
                    ReceiveComPort(ref Answer, 5000, true);
                    if (!Answer.Contains("W " + LPC_RAMBASE_LPC11XX + " " + CopyLength + "\r\n0"))
                    {
                        Console.WriteLine("Wrong answer on Write-Command");
                        return;
                    }

                    Console.Write(".");

                    {
                        block_CRC = 0;
                        Line = 0;

                        // Transfer blocks of 45 * 4 bytes to RAM
                        for (uint Pos = SectorStart + SectorOffset; (Pos < SectorStart + SectorOffset + CopyLength) && (Pos < ISP_ENVIRONMENT.binaryLength); Pos += (45 * 4))
                        {
                            for (uint Block = 0; Block < 4; Block++)  // Each block 45 bytes
                            {
                                Console.Write(".");

                                uint tmpStringPos = 0;

                                sendbuf[Line][tmpStringPos++] = (char)(' ' + 45);

                                for (uint BlockOffset = 0; BlockOffset < 45; BlockOffset++)
                                {
                                    

                                    if ((ISP_ENVIRONMENT.binaryOffset < LPC_RAMSTART_LPC11XX)
                                    || (ISP_ENVIRONMENT.binaryOffset >= (uint)LPC_RAMSTART_LPC11XX + (LPCTypes.RAMSize * 1024)))
                                    { // Flash: use full memory
                                        if ((Pos + Block * 45 + BlockOffset) < ISP_ENVIRONMENT.binaryLength)
                                        {
                                            c = ISP_ENVIRONMENT.binaryContent[Pos + Block * 45 + BlockOffset];
                                        }
                                        else
                                        {
                                            c = 0;
                                        }
                                    }
                                    else
                                    { // RAM: Skip first 0x200 bytes, these are used by the download program in LPC21xx
                                        c = ISP_ENVIRONMENT.binaryContent[Pos + Block * 45 + BlockOffset + 0x200];
                                    }

                                    //if (Sector == 0)
                                    //{
                                    //    Console.WriteLine(block_CRC + ", " + (Pos + Block * 45 + BlockOffset)+", " + ISP_ENVIRONMENT.binaryContent[Pos + Block * 45 + BlockOffset]);
                                    //}
                                    block_CRC += (uint)c;

                                    k = (k << 8) + (c & 255);

                                    if ((BlockOffset % 3) == 2)   // Collecting always 3 Bytes, then do processing in 4 Bytes
                                    {
                                        sendbuf[Line][tmpStringPos++] = uuencode_table[(k >> 18) & 63];
                                        sendbuf[Line][tmpStringPos++] = uuencode_table[(k >> 12) & 63];
                                        sendbuf[Line][tmpStringPos++] = uuencode_table[(k >> 6) & 63];
                                        sendbuf[Line][tmpStringPos++] = uuencode_table[k & 63];
                                    }
                                }

                                sendbuf[Line][tmpStringPos++] = '\r';
                                sendbuf[Line][tmpStringPos++] = '\n';
                                sendbuf[Line][tmpStringPos++] = (char)0;

                                string sendBufLine = new string(sendbuf[Line]);
                                sendBufLine = sendBufLine.Replace("\0", "");
                                SendComPort(sendBufLine);
                                ReceiveComPort(ref Answer, 5000, false);
                                if(!Answer.Contains(sendBufLine))
                                {
                                    Console.WriteLine("Error on writing data (1)");
                                    return;
                                }

                                Line++;

                                //Console.WriteLine("Line = " + Line);

                                if (Line == 20)
                                {
                                    int repeat;
                                    for (repeat = 0; repeat < 3; repeat++)
                                    {
                                        SendComPort(block_CRC + "\r\n");
                                        ReceiveComPort(ref Answer, 5000, true);

                                        if(!Answer.Contains(block_CRC + "\r\nOK\r\n"))
                                        {
                                            for (int i = 0; i < Line; i++)
                                            {
                                                string sendBufStr = new string(sendbuf[i]);
                                                sendBufStr = sendBufStr.Replace("\0", "");
                                                SendComPort(sendBufStr);
                                                ReceiveComPort(ref Answer, 5000, false);
                                            }
                                        }
                                        else
                                        {
                                            break;
                                        }

                                    }

                                    if (repeat >= 3)
                                    {
                                        Console.WriteLine("Error on writing block_CRC (1)");
                                        return;
                                    }
                                    Line = 0;
                                    block_CRC = 0;
                                }

                            }
                        }

                        if (Line != 0)
                        {
                            int repeat;

                            for (repeat = 0; repeat < 3; repeat++)
                            {
                                SendComPort(block_CRC + "\r\n");
                                ReceiveComPort(ref Answer, 5000, true);

                                if (!Answer.Contains(block_CRC + "\r\nOK\r\n"))
                                {
                                    for (int i = 0; i < Line; i++)
                                    {
                                        string sendBufStr = new string(sendbuf[i]);
                                        sendBufStr = sendBufStr.Replace("\0", "");
                                        SendComPort(sendBufStr);
                                        ReceiveComPort(ref Answer, 5000, false);
                                    }
                                }
                                else
                                {
                                    break;
                                }

                            }

                            if (repeat >= 3)
                            {
                                Console.WriteLine("Error on writing block_CRC (3)");
                                return;
                            }

                        }
                    }

                    if ((ISP_ENVIRONMENT.binaryOffset < LPC_RAMSTART_LPC11XX)
                    || (ISP_ENVIRONMENT.binaryOffset >= (uint)LPC_RAMSTART_LPC11XX + (LPCTypes.RAMSize * 1024)))
                    {
                        SendComPort("P " + Sector + " " + Sector + "\r\n");

                        ReceiveComPort(ref Answer, 5000, true);

                        if (!Answer.Contains("P " + Sector + " " + Sector + "\r\n0\r\n"))
                        {
                            Console.WriteLine("Wrong answer on Prepare-Command (2) (Sector "+ Sector +")");
                            return;
                        }

                        // Round CopyLength up to one of the following values: 512, 1024,
                        // 4096, 8192; but do not exceed the maximum copy size (usually
                        // 8192, but chip-dependent)
                        if (CopyLength < 512)
                        {
                            CopyLength = 512;
                        }
                        else if (SectorLength < 1024)
                        {
                            CopyLength = 1024;
                        }
                        else if (SectorLength < 4096)
                        {
                            CopyLength = 4096;
                        }
                        else
                        {
                            CopyLength = 8192;
                        }
                        if (CopyLength > LPCTypes.MaxCopySize)
                        {
                            CopyLength = LPCTypes.MaxCopySize;
                        }

                        string cStringToSend = "C " + (ISP_ENVIRONMENT.binaryOffset + SectorStart + SectorOffset) + " " + LPC_RAMBASE_LPC11XX + " " + CopyLength + "\r\n";
                        SendComPort(cStringToSend);
                        ReceiveComPort(ref Answer, 5000, true);

                        if (!Answer.Contains(cStringToSend+"0\r\n"))        
                        {
                            Console.WriteLine("Wrong answer on Copy-Command");
                            return;
                        }
                    }

                }

                Console.Write("\n");

                if ((SectorStart + SectorLength) >= ISP_ENVIRONMENT.binaryLength && Sector != 0)
                {
                    Sector = 0;
                    SectorStart = 0;
                }
                else if (Sector == 0)
                {
                    break;
                }
                else
                {
                    SectorStart += LPCTypes.SectorTable[Sector];
                    Sector++;
                }
            }

            Console.WriteLine("Download Finished");
            Console.WriteLine("Now launching the brand new code");
            SendComPort("W 268435456 16\r\n");
            Thread.Sleep(100);
            SendComPort("0`4@\"20%@_N<,[0#@!`#Z!0``\r\n");
            Thread.Sleep(100);
            SendComPort("1462\r\n");
            Thread.Sleep(100);
            SendComPort("G 268435456 T\r\n");
            Thread.Sleep(100);
        }

    }
}
