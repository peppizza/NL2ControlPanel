using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace TCPClient
{
    /**
     * Example Java client program with text user interface for demonstrating the Telemetry Interface introduced in NoLimits 2.2.0.0
     *
     * Client Demo Version 0.41
     * Copyright 2017 Ole Lange
     *
     * The protocol is a binary message based protocol using TCP. Each message will be answered by the server with a specific message in the same order as messages were sent by the client.
     * Each message has the same basic binary format. All multi-byte values are in network-byte-order (big-endian)!!
     * The minimum message size is 10 bytes.
     *
     * Default TCP port of NoLimits 2 telemetry server is: 15151
     * Telemetry server needs to be enabled by starting NoLimits2 with command line parameter '--telemetry'
     * Port can be changed with command line parameter '--telemetryport=<port>'
     *
     * E.g. for starting the server on port 15152, start with commandline: '"c:\Program files\NoLimits 2\64bit\NoLimits2app.exe" --telemetry --telemetryport=15152'
     * 
     * Message Format Structure:
     * ByteOffset  Type      Size      Meaning 
     * ====================================================================
     * 0           uchar8    1         Message Start (magic number, value = 'N')
     * 1           ushort16  2         MessageType (see message enum table)
     * 3           uint32    4         Request ID (can be freely assigned by client)
     * 7           ushort16  2         DataSize (depends on message type)
     * 9           varying   DataSize  Depends on message type
     * 9+DataSize  uchar8    1         Message End (magic number, value = 'L')
     */
    public class NL2TelemetryClient
    {
        private string _server;
        private int _port;
        private readonly TcpClient _client;
        //NetworkStream stream = ClientSocket.GetStream();
        /**
       * Message Enum
       * Type: Request
       * Can be send by the client to keep connetion alive. No other purpose. Returned message by server is MSG_OK
       * DataSize = 0
       */
        private const int N_MSG_IDLE = 0;

        /**
       * Message Enum
       * Type: Reply
       * Typical answer from server for messages that were successfully processed and do not require a specific returned answer
       * DataSize = 0
       */
        private const int N_MSG_OK = 1;

        /**
       * Message Enum
       * Type: Reply   
       * Will be send by the server in case of an error. The data component contains an UTF-8 encoded error message
       * DataSize = number of bytes of UTF8 encoded string
       *    UTF8 string
       */
        private const int N_MSG_ERROR = 2;

        /**
       * Message Enum
       * Type: Request   
       * Can be used by the client to request the application version. The server will reply with MSG_VERSION
       * DataSize = 0
       */
        private const int N_MSG_GET_VERSION = 3;

        /**
       * Message Enum
       * Type: Reply   
       * Will be send by the server as an answer to MSG_GET_VERSION
       * DataSize = 4 
       *    4 Bytes (major to minor version numbers e.g. 2, 2, 0, 0 for '2.2.0.0')
       */
        private const int N_MSG_VERSION = 4;

        /**
       * Message Enum
       * Type: Request   
       * Can be used by the client to request common telemetry data. The server will reply with MSG_TELEMETRY
       * DataSize = 0
       */
        private const int N_MSG_GET_TELEMETRY = 5;

        /**
       * Message Enum
       * Type: Reply   
       * Will be send by the server as an anwser to MSG_GET_TELEMETRY
       * DataSize = 76 
       *    int32 (state flags)
       *      bit0 -> in play mode
       *      bit1 -> braking
       *      bit2 -> pause state
       *      bit3-31 -> reserved
       *    int32 (current rendered frame number) -> can be used to detect if telemetry data is new
       *    int32 (view mode)
       *    int32 (current coaster)
       *    int32 (coaster style id)
       *    int32 (current train)
       *    int32 (current car)
       *    int32 (current seat)
       *    float32 (speed)
       *    float32 (Position x)
       *    float32 (Position y)
       *    float32 (Position z)
       *    float32 (Rotation quaternion x)
       *    float32 (Rotation quaternion y)
       *    float32 (Rotation quaternion z)
       *    float32 (Rotation quaternion w)
       *    float32 (G-Force x)
       *    float32 (G-Force y)
       *    float32 (G-Force z)   
       */
        private const int N_MSG_TELEMETRY = 6;

        /**
       * Message Enum
       * Type: Request   
       * Can be used by the client to request the number of coasters. The server will reply with MSG_INT_VALUE
       * DataSize = 0
       */
        private const int N_MSG_GET_COASTER_COUNT = 7;

        /**
       * Message Enum
       * Type: Reply   
       * Will be send by the server as an answer to messages requesting various numbers
       * DataSize = 4
       *    int32 (meaning depends on requested information)
       */
        private const int N_MSG_INT_VALUE = 8;

        /**
       * Message Enum
       * Type: Request   
       * Can be used by the client to request the name of a specific coaster. The server will reply with MSG_STRING
       * DataSize = 4
       *    int32 (coaster index 0..N-1), use MSG_GET_COASTER_COUNT to query the number of available coasters
       */
        private const int N_MSG_GET_COASTER_NAME = 9;

        /**
       * Message Enum 
       * Type: Reply
       * Will be send by the server as an answer to messages requesting various strings
       * DataSize = length of UTF8 encoded string
       *    UTF8 string
       */
        private const int N_MSG_STRING = 10;

        /**
       * Message Enum
       * Type: Request
       * Can be used by the client to request the current coaster and nearest station indices. The server will reply with MSG_INT_VALUE_PAIR
       * DataSize = 0
       */
        private const int N_MSG_GET_CURRENT_COASTER_AND_NEAREST_STATION = 11;

        /**
       * Message Enum
       * Type: Reply
       * Will be send by the server as an answer to messages requesting various value pairs
       * DataSize = 8
       *    int32 (first value)
       *    int32 (second value)
       */
        private const int N_MSG_INT_VALUE_PAIR = 12;

        /**
       * Message Enum
       * Type: Request
       * Can be used by the client to set the emergency stop. The server will reply with MSG_OK
       * DataSize = 5
       *    int32 (coaster index)
       *    uchar8 (1 = on, 0 = off)
       */
        private const int N_MSG_SET_EMERGENCY_STOP = 13;

        /**
       * Message Enum 
       * Type: Request
       * Can be used by the client to request the state of a specific station. The server will reply with MSG_STATION_STATE
       * DataSize = 8
       *    int32 (coaster index)
       *    int32 (station index (from MSG_GET_CURRENT_COASTER_AND_NEAREST_STATION))
       */
        private const int N_MSG_GET_STATION_STATE = 14;

        /**
       * Message Enum
       * Type: Reply
       * Will be send by server as an answer to a MSG_GET_STATION_STATE messge
       * DataSize = 4
       *    int32 (flags)
       *      bit0 -> E-Stop On/Off
       *      bit1 -> Manual Dispatch On/Off
       *      bit2 -> Can Dispatch
       *      bit3 -> Can Close Gates
       *      bit4 -> Can Open Gates
       *      bit5 -> Can Close Harness 
       *      bit6 -> Can Open Harness
       *      bit7 -> Can Raise Platform
       *      bit8 -> Can Lower Platform
       *      bit9 -> Can Lock Flyer Car
       *      bit10 -> Can Unlock Flyer Car
       *      bit11 -> There is a train in station that has stopped
       *      bit12 -> The train inside the station is the current train of the ride view
       *      bit13-31 -> reserved
       */
        private const int N_MSG_STATION_STATE = 15;

        /**
       * Message Enum
       * Type: Request
       * Can be used by the client to switch between manual and automatic station mode
       * DataSize = 9
       *    int32 (coaster index)
       *    int32 (station index)
       *    uchar8 (1 = on, 0 = off)
       */
        private const int N_MSG_SET_MANUAL_MODE = 16;

        /**
       * Message Enum
       * Type: Request
       * Can be used by the client to dispatch a train in manual mode
       * DataSize = 8
       *    int32 (coaster index)
       *    int32 (station index)
       */
        private const int N_MSG_DISPATCH = 17;

        /**
       * Message Enum
       * Type: Request
       * Can be used by the client to change gates in manual mode
       * DataSize = 9
       *    int32 (coaster index)
       *    int32 (station index)
       *    uchar8 (1 = on, 0 = off)
       */
        private const int N_MSG_SET_GATES = 18;

        /**
       * Message Enum
       * Type: Request
       * Can be used by the client to change harness in manual mode
       * DataSize = 9
       *    int32 (coaster index)
       *    int32 (station index)
       *    uchar8 (1 = on, 0 = off)
       */
        private const int N_MSG_SET_HARNESS = 19;

        /**
       * Message Enum
       * Type: Request
       * Can be used by the client to lower/raise platform in manual mode
       * DataSize = 9
       *    int32 (coaster index)
       *    int32 (station index)
       *    uchar8 (1 = on, 0 = off)
       */
        private const int N_MSG_SET_PLATFORM = 20;

        /**
       * Message Enum
       * Type: Request
       * Can be used by the client to lock/unlock flyer car in manual mode
       * DataSize = 9
       *    int32 (coaster index)
       *    int32 (station index)
       *    uchar8 (1 = on, 0 = off)
       */
        private const int N_MSG_SET_FLYER_CAR = 21;

        /**
       * Message Enum
       * Type: Request
       * Can be used by the client to load and start a park (Only supported when used with an Attraction License)
       * DataSize = 1 + utf8 string length
       *    uchar8 start type (0 = normal, 1 = started in paused state)
       *    utf8 string (park file path, must use internal string represention (with '/' as file separator))
       *                (A: path can be internal path taken from Park Library, e.g. "intern:parks/Hybris/Hybris.nl2park")
       *                (B: path can be relative to application base folder, e.g. "parks/Hybris/Hybris.nl2park")
       *                (C: path can be absolute. Windows absolute paths need to be specified like this: "/c:/Program files/NoLimits 2/parks/Hybris/Hybris.nl2park")
       */
        private const int N_MSG_LOAD_PARK = 24;

        /**
       * Message Enum
       * Type: Request
       * Can be used by the client to close the currently loaded or running park (Only supported when used with an Attraction License)
       * DataSize = 0
       */
        private const int N_MSG_CLOSE_PARK = 25;

        /**
       * Message Enum
       * Type: Request
       * Can be used by the client to request the server to quit. The connection to the server will be lost after sending this message. (Only supported when used with an Attraction License)
       * DataSize = 0
       */
        private const int N_MSG_QUIT_SERVER = 26;

        /**
       * Message Enum
       * Type: Request
       * Can be used by the client to change the pause state in play mode (Only supported when used with an Attraction License)
       * DataSize = 1
       *    uchar8 (1 = pause on, 0 = pause off)
       */
        private const int N_MSG_SET_PAUSE = 27;

        /**
       * Message Enum
       * Type: Request
       * Can be used by the client to stop and restart a park (Only supported when used with an Attraction License)
       * DataSize = 1
       *    uchar8 start type (0 = normal, 1 = started with pause state)
       */
        private const int N_MSG_RESET_PARK = 28;

        /**
       * Message Enum
       * Type: Request
       * Can be used by the client to select a specific seat (Only supported when used with an Attraction License)
       * DataSize = 16
       *    int32 (coaster index)
       *    int32 (train index)
       *    int32 (car index)
       *    int32 (seat index)
       */
        private const int N_MSG_SELECT_SEAT = 29;

        /**
       * Message Enum
       * Type: Request
       * Can be used by the client to disable some things that may disturb working as an attraction (random station wait times, annoying display texts) (Only supported when used with an Attraction License)
       * DataSize = 1
       *    uchar8 (1 = attraction mode on, 0 = normal behaviour)
       */
        private const int N_MSG_SET_ATTRACTION_MODE = 30;

        /**
       * Message Enum
       * Type: Request
       * Can be used by the client to recenter VR
       * DataSize = 0
       */
        private const int N_MSG_RECENTER_VR = 31;

        /**
       * Message Enum
       * Type: Request
       * Can be used by the client to set a custom view 
       * DataSize = 21
       *    float32 (pos.x in meters)
       *    float32 (pos.y in meters)
       *    float32 (pos.z in meters)
       *    float32 (azimuth in degrees)
       *    float32 (elevation in degrees)
       *    bool (false = flyview, true = walkview, )
       */
        private const int N_MSG_SET_CUSTOM_VIEW = 32;

        /**
       * Start of extra size data within message
       */
        private const int c_nExtraSizeOffset = 9;

        /**
       * The request ID can be freely assigned by the client, we simply use an increasing counter.
       * The server will reply to request messages using the same id.
       * The request ID can be used to identify matching replys from the server to requests from the client.
       */
        private static int s_nRequestId;

        public NL2TelemetryClient(string server, int port)
        {
            _server = server;
            _port = port;
            _client = new TcpClient(server, port);
        }

        public void Close()
        {
            _client.Close();
        }
        public void SendCommand(string command)
        {
            var stream = _client.GetStream();
            Console.WriteLine("sending {0}", command);
            var bytes = decodeCommand(command);
            stream.Write(bytes, 0, bytes.Length);
            var reader = new BinaryReader(stream);
            bytes = readMessage(reader);
            decodeMessage(bytes);
        }
        
        public void Infinite()
        {
            try
            {
                NetworkStream stream = _client.GetStream();
                Console.WriteLine("...connected!");

                Console.WriteLine("Enter 'help' for list of available commands");
                while (_client.Connected)
                {
                    Console.Write("Enter command: ");
                    string sentence = Console.ReadLine();
                    if (sentence == null || sentence.Equals("quit") || sentence.Equals("exit"))
                    {
                        break;
                    }

                    byte[] bytes = decodeCommand(sentence);
                    if (bytes != null)
                    {
                        stream.Write(bytes, 0, bytes.Length);

                        var reader = new BinaryReader(stream);

                        bytes = readMessage(reader);

                        decodeMessage(bytes);

                    }
                }

                stream.Close();
                _client.Close();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /**
       * Encode n as two bytes (ushort16/sshort16) in network byte order (big-endian)
       */
        private static void encodeUShort16(byte[] msg, int offset, int n)
        {
            msg[offset] = (byte) ((n >> 8) & 0xFF);
            msg[offset + 1] = (byte) (n & 0xFF);
        }

        /**
       * Encode n as four bytes (uint32/int32) in network byte order (big-endian)
       */
        private static void encodeInt32(byte[] msg, int offset, int n)
        {
            msg[offset] = (byte) ((n >> 24) & 0xFF);
            msg[offset + 1] = (byte) ((n >> 16) & 0xFF);
            msg[offset + 2] = (byte) ((n >> 8) & 0xFF);
            msg[offset + 3] = (byte) (n & 0xFF);
        }


        /**
       * Encode f as four bytes (IEEE 32bit float) in network byte order (big-endian)
       */
        private static void encodeFloat32(byte[] msg, int offset, float f)
        {
            encodeInt32(msg, offset, BitConverter.ToInt32(BitConverter.GetBytes(f), 0));
        }

        /**
       * Encode b as one byte
       */
        private static void encodeBoolean(byte[] msg, int offset, bool b)
        {
            msg[offset] = (byte) (b ? 1 : 0);
        }

        /**
       * Encode extra bytes
       */
        private static void encodeExtraBytes(byte[] msg, int offset, byte[] extraBytes)
        {
            int c = extraBytes.Length;
            for (int i = 0; i < c; ++i)
            {
                msg[offset + i] = extraBytes[i];
            }
        }

        /**
       * Decode one byte as bool
       */
        private static bool decodeBoolean(byte[] msg, int offset)
        {
            return msg[offset] != 0;
        }

        /**
       * Decode two bytes in network byte order (big-endian) as ushort16
       */
        private static int decodeUShort16(byte b1, byte b2)
        {
            int n1 = (((int) b1) & 0xFF) << 8;
            int n2 = ((int) b2) & 0xFF;
            return n1 | n2;
        }

        /**
       * Decode four bytes in network byte order (big-endian) as int32
       */
        private static int decodeInt32(byte[] msg, int offset)
        {
            int n1 = (((int) msg[offset]) & 0xFF) << 24;
            int n2 = (((int) msg[offset + 1]) & 0xFF) << 16;
            int n3 = (((int) msg[offset + 2]) & 0xFF) << 8;
            int n4 = ((int) msg[offset + 3]) & 0xFF;
            return n1 | n2 | n3 | n4;
        }

        /**
       * Decode four bytes in network byte order (big-endian) as float32
       */
        private static float decodeFloat(byte[] msg, int offset)
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(decodeInt32(msg, offset)));
        }

        /**
       * Decode len bytes as UTF8-string
       */
        private static String decodeString(byte[] msg, int offset, int len)
        {
            string str;

            try
            {
                str = Encoding.UTF8.GetString(msg, offset, len);
            }
            catch (EncoderFallbackException)
            {
                str = null;
            }

            return str;
        }

        /**
       * Create a message with DataSize=0
       */
        private static byte[] createSimpleMessage(int requestId, int msgEnum)
        {
            byte[] msg = new byte[10];
            msg[0] = (byte) 'N';
            encodeUShort16(msg, 1, msgEnum);
            encodeInt32(msg, 3, requestId);
            encodeUShort16(msg, 7, 0);
            msg[9] = (byte) 'L';
            return msg;
        }

        private static byte[] createComplexMessage(int requestId, int msgEnum, int extraSize)
        {
            if (extraSize < 0 || extraSize > 65535) return null;
            byte[] msg = new byte[10 + extraSize];
            msg[0] = (byte) 'N';
            encodeUShort16(msg, 1, msgEnum);
            encodeInt32(msg, 3, requestId);
            encodeUShort16(msg, 7, extraSize);
            msg[9 + extraSize] = (byte) 'L';
            return msg;
        }

        private static byte[] createSpecialStringMessage(int requestId, int msgEnum, int bytesBeforeString, string str)
        {
            byte[] utf8Str = null;

            try
            {
                utf8Str = Encoding.UTF8.GetBytes(str);
            }
            catch (EncoderFallbackException e)
            {
                Console.WriteLine(e);
                return null;
            }

            byte[] msg = createComplexMessage(requestId, msgEnum, bytesBeforeString + utf8Str.Length);
            encodeExtraBytes(msg, c_nExtraSizeOffset + bytesBeforeString, utf8Str);
            return msg;
        }

        private static byte[] createStringMessage(int requestId, String str)
        {
            return createSpecialStringMessage(requestId, N_MSG_STRING, 0, str);
        }

        private static byte[] createBoolMessage(int requestId, int msgEnum, bool bVal)
        {
            byte[] msg = createComplexMessage(requestId, msgEnum, 1);
            encodeBoolean(msg, c_nExtraSizeOffset, bVal);
            return msg;
        }

        /**
       * Show information about required syntax of program
       */
        private static void printSyntax()
        {
            Console.WriteLine("Syntax: NL2TelemetryClient server [port]");
        }

        /**
       * Parse command and create correspondig message
       */
        private static byte[] decodeCommand(string In)
        {
            if (In.Equals("i") || In.Equals("idle"))
            {
                return createSimpleMessage(s_nRequestId++, N_MSG_IDLE);
            }
            else if (In.Equals("gv") || In.Equals("getversion"))
            {
                return createSimpleMessage(s_nRequestId++, N_MSG_GET_VERSION);
            }
            else if (In.Equals("gt") || In.Equals("gettelemetry"))
            {
                return createSimpleMessage(s_nRequestId++, N_MSG_GET_TELEMETRY);
            }
            else if (In.Equals("gcc") || In.Equals("getcoastercount"))
            {
                return createSimpleMessage(s_nRequestId++, N_MSG_GET_COASTER_COUNT);
            }
            else if (In.Equals("gcn") || In.Equals("getcoastername"))
            {
                int nCoasterIndex = 0; // first coaster
                byte[] msg = createComplexMessage(s_nRequestId++, N_MSG_GET_COASTER_NAME, 4);
                encodeInt32(msg, c_nExtraSizeOffset, nCoasterIndex);
                return msg;
            }
            else if (In.Equals("gccns") || In.Equals("getcurrentcoasterandneareststation"))
            {
                return createSimpleMessage(s_nRequestId++, N_MSG_GET_CURRENT_COASTER_AND_NEAREST_STATION);
            }
            else if (In.Equals("seon") || In.Equals("setemergencystopon"))
            {
                int nCoasterIndex = 0; // first coaster
                byte[] msg = createComplexMessage(s_nRequestId++, N_MSG_SET_EMERGENCY_STOP, 5);
                encodeInt32(msg, c_nExtraSizeOffset, nCoasterIndex);
                encodeBoolean(msg, c_nExtraSizeOffset + 4, true);
                return msg;
            }
            else if (In.Equals("seoff") || In.Equals("setemergencystopoff"))
            {
                int nCoasterIndex = 0; // first coaster
                byte[] msg = createComplexMessage(s_nRequestId++, N_MSG_SET_EMERGENCY_STOP, 5);
                encodeInt32(msg, c_nExtraSizeOffset, nCoasterIndex);
                encodeBoolean(msg, c_nExtraSizeOffset + 4, false);
                return msg;
            }
            else if (In.Equals("gss") || In.Equals("getstationstate"))
            {
                int nCoasterIndex = 0; // first coaster
                int nStationIndex = 0; // first station of coaster
                byte[] msg = createComplexMessage(s_nRequestId++, N_MSG_GET_STATION_STATE, 8);
                encodeInt32(msg, c_nExtraSizeOffset, nCoasterIndex);
                encodeInt32(msg, c_nExtraSizeOffset + 4, nStationIndex);
                return msg;
            }
            else if (In.Equals("setsmmon") || In.Equals("setstationmanualmodeon"))
            {
                int nCoasterIndex = 0; // first coaster
                int nStationIndex = 0; // first station of coaster
                byte[] msg = createComplexMessage(s_nRequestId++, N_MSG_SET_MANUAL_MODE, 9);
                encodeInt32(msg, c_nExtraSizeOffset, nCoasterIndex);
                encodeInt32(msg, c_nExtraSizeOffset + 4, nStationIndex);
                encodeBoolean(msg, c_nExtraSizeOffset + 8, true);
                return msg;
            }
            else if (In.Equals("setsmmoff") || In.Equals("setstationmanualmodeoff"))
            {
                int nCoasterIndex = 0; // first coaster
                int nStationIndex = 0; // first station of coaster
                byte[] msg = createComplexMessage(s_nRequestId++, N_MSG_SET_MANUAL_MODE, 9);
                encodeInt32(msg, c_nExtraSizeOffset, nCoasterIndex);
                encodeInt32(msg, c_nExtraSizeOffset + 4, nStationIndex);
                encodeBoolean(msg, c_nExtraSizeOffset + 8, false);
                return msg;
            }
            else if (In.Equals("d") || In.Equals("dispatch"))
            {
                int nCoasterIndex = 0; // first coaster
                int nStationIndex = 0; // first station of coaster
                byte[] msg = createComplexMessage(s_nRequestId++, N_MSG_DISPATCH, 8);
                encodeInt32(msg, c_nExtraSizeOffset, nCoasterIndex);
                encodeInt32(msg, c_nExtraSizeOffset + 4, nStationIndex);
                return msg;
            }
            else if (In.Equals("sgc") || In.Equals("stationgatesclose"))
            {
                int nCoasterIndex = 0; // first coaster
                int nStationIndex = 0; // first station of coaster
                byte[] msg = createComplexMessage(s_nRequestId++, N_MSG_SET_GATES, 9);
                encodeInt32(msg, c_nExtraSizeOffset, nCoasterIndex);
                encodeInt32(msg, c_nExtraSizeOffset + 4, nStationIndex);
                encodeBoolean(msg, c_nExtraSizeOffset + 8, false);
                return msg;
            }
            else if (In.Equals("sgo") || In.Equals("stationgatesopen"))
            {
                int nCoasterIndex = 0; // first coaster
                int nStationIndex = 0; // first station of coaster
                byte[] msg = createComplexMessage(s_nRequestId++, N_MSG_SET_GATES, 9);
                encodeInt32(msg, c_nExtraSizeOffset, nCoasterIndex);
                encodeInt32(msg, c_nExtraSizeOffset + 4, nStationIndex);
                encodeBoolean(msg, c_nExtraSizeOffset + 8, true);
                return msg;
            }
            else if (In.Equals("shc") || In.Equals("stationharnessclose"))
            {
                int nCoasterIndex = 0; // first coaster
                int nStationIndex = 0; // first station of coaster
                byte[] msg = createComplexMessage(s_nRequestId++, N_MSG_SET_HARNESS, 9);
                encodeInt32(msg, c_nExtraSizeOffset, nCoasterIndex);
                encodeInt32(msg, c_nExtraSizeOffset + 4, nStationIndex);
                encodeBoolean(msg, c_nExtraSizeOffset + 8, false);
                return msg;
            }
            else if (In.Equals("sho") || In.Equals("stationharnessopen"))
            {
                int nCoasterIndex = 0; // first coaster
                int nStationIndex = 0; // first station of coaster
                byte[] msg = createComplexMessage(s_nRequestId++, N_MSG_SET_HARNESS, 9);
                encodeInt32(msg, c_nExtraSizeOffset, nCoasterIndex);
                encodeInt32(msg, c_nExtraSizeOffset + 4, nStationIndex);
                encodeBoolean(msg, c_nExtraSizeOffset + 8, true);
                return msg;
            }
            else if (In.Equals("spr") || In.Equals("stationplatformraise"))
            {
                int nCoasterIndex = 0; // first coaster
                int nStationIndex = 0; // first station of coaster
                byte[] msg = createComplexMessage(s_nRequestId++, N_MSG_SET_PLATFORM, 9);
                encodeInt32(msg, c_nExtraSizeOffset, nCoasterIndex);
                encodeInt32(msg, c_nExtraSizeOffset + 4, nStationIndex);
                encodeBoolean(msg, c_nExtraSizeOffset + 8, false);
                return msg;
            }
            else if (In.Equals("spl") || In.Equals("stationplatformlower"))
            {
                int nCoasterIndex = 0; // first coaster
                int nStationIndex = 0; // first station of coaster
                byte[] msg = createComplexMessage(s_nRequestId++, N_MSG_SET_PLATFORM, 9);
                encodeInt32(msg, c_nExtraSizeOffset, nCoasterIndex);
                encodeInt32(msg, c_nExtraSizeOffset + 4, nStationIndex);
                encodeBoolean(msg, c_nExtraSizeOffset + 8, true);
                return msg;
            }
            else if (In.Equals("sfl") || In.Equals("stationflyercarlock"))
            {
                int nCoasterIndex = 0; // first coaster
                int nStationIndex = 0; // first station of coaster
                byte[] msg = createComplexMessage(s_nRequestId++, N_MSG_SET_FLYER_CAR, 9);
                encodeInt32(msg, c_nExtraSizeOffset, nCoasterIndex);
                encodeInt32(msg, c_nExtraSizeOffset + 4, nStationIndex);
                encodeBoolean(msg, c_nExtraSizeOffset + 8, false);
                return msg;
            }
            else if (In.Equals("sfu") || In.Equals("stationflyercarunlock"))
            {
                int nCoasterIndex = 0; // first coaster
                int nStationIndex = 0; // first station of coaster
                byte[] msg = createComplexMessage(s_nRequestId++, N_MSG_SET_FLYER_CAR, 9);
                encodeInt32(msg, c_nExtraSizeOffset, nCoasterIndex);
                encodeInt32(msg, c_nExtraSizeOffset + 4, nStationIndex);
                encodeBoolean(msg, c_nExtraSizeOffset + 8, true);
                return msg;
            }
            else if (In.Equals("loadpark"))
            {
                String path = "intern:parks/Contributed/Fenrir.nl2pkg";
                // Relative paths need to be specified like this: "parks/Contributed/Fenrir.nl2pkg";
                // Windows absolute paths need to be specified like this: "/C:/Program Files/NoLimits 2/parks/Contributed/Fenrir.nl2pkg";
                byte[] msg = createSpecialStringMessage(s_nRequestId++, N_MSG_LOAD_PARK, 1, path);
                msg[c_nExtraSizeOffset] = 0; // default start 
                return msg;
            }
            else if (In.Equals("loadparkpaused"))
            {
                String path = "intern:parks/Hybris/Hybris.nl2park";
                // Relative paths need to be specified like this: "parks/Contributed/Fenrir.nl2pkg";
                // Windows absolute paths need to be specified like this: "/C:/Program Files/NoLimits 2/parks/Contributed/Fenrir.nl2pkg";
                byte[] msg = createSpecialStringMessage(s_nRequestId++, N_MSG_LOAD_PARK, 1, path);
                msg[c_nExtraSizeOffset] = 1; // start in paused state
                return msg;
            }
            else if (In.Equals("closepark"))
            {
                return createSimpleMessage(s_nRequestId++, N_MSG_CLOSE_PARK);
            }
            else if (In.Equals("quitserver"))
            {
                return createSimpleMessage(s_nRequestId++, N_MSG_QUIT_SERVER);
            }
            else if (In.Equals("pause"))
            {
                return createBoolMessage(s_nRequestId++, N_MSG_SET_PAUSE, true);
            }
            else if (In.Equals("unpause"))
            {
                return createBoolMessage(s_nRequestId++, N_MSG_SET_PAUSE, false);
            }
            else if (In.Equals("resetpark"))
            {
                return createBoolMessage(s_nRequestId++, N_MSG_RESET_PARK, true);
            }
            else if (In.Equals("changeseat"))
            {
                int nCoasterIndex = 0; // first coaster
                int nTrainIndex = 0; // first train
                int nCarIndex = 0; // first car
                int nSeatIndex = 1; // second seat
                byte[] msg = createComplexMessage(s_nRequestId++, N_MSG_SELECT_SEAT, 16);
                encodeInt32(msg, c_nExtraSizeOffset, nCoasterIndex);
                encodeInt32(msg, c_nExtraSizeOffset + 4, nTrainIndex);
                encodeInt32(msg, c_nExtraSizeOffset + 8, nCarIndex);
                encodeInt32(msg, c_nExtraSizeOffset + 12, nSeatIndex);
                return msg;
            }
            else if (In.Equals("attractionmode"))
            {
                return createBoolMessage(s_nRequestId++, N_MSG_SET_ATTRACTION_MODE, true);
            }
            else if (In.Equals("recentervr"))
            {
                return createSimpleMessage(s_nRequestId++, N_MSG_RECENTER_VR);
            }
            else if (In.Equals("setcustomview"))
            {
                float posx = 10.0f; // meters
                float posy = 3.0f; // meters
                float posz = 100.0f; // meters
                float azimuth = 90.0f; // degrees (0 = north)
                float elevation = 43.0f; // degrees
                bool walkView = false; // true -> walk view, false -> fly view
                byte[] msg = createComplexMessage(s_nRequestId++, N_MSG_SET_CUSTOM_VIEW, 21);
                encodeFloat32(msg, c_nExtraSizeOffset + 0, posx);
                encodeFloat32(msg, c_nExtraSizeOffset + 4, posy);
                encodeFloat32(msg, c_nExtraSizeOffset + 8, posz);
                encodeFloat32(msg, c_nExtraSizeOffset + 12, azimuth);
                encodeFloat32(msg, c_nExtraSizeOffset + 16, elevation);
                encodeBoolean(msg, c_nExtraSizeOffset + 20, walkView);
                return msg;
            }
            else if (!In.Equals("help"))
            {
                Console.WriteLine("Invalid command '" + In + "'\n");
            }

            printValidCommands();
            return null;
        }

        /**
       * Show available command list
       */
        private static void printValidCommands()
        {
            Console.WriteLine("Valid commands: i (idle) - Send idle message");
            Console.WriteLine("                help - Show available commands");
            Console.WriteLine("                quit/exit - Leave client program");
            Console.WriteLine("                gv (getversion) - Query server version");
            Console.WriteLine("                gt (gettelemetry) - Query telemetry data");
            Console.WriteLine("                gcc (getcoastercount) - Query number of coasters");
            Console.WriteLine("                gcn (getcoastername) - Query name of first coaster");
            Console.WriteLine(
                "                gccns (getcurrentcoasterandneareststation) - Query current coaster index and nearest station");
            Console.WriteLine("                seon (setemergencystopon) - Enable e-stop on first coaster");
            Console.WriteLine("                seoff (setemergencystopoff) - Disable e-stop on first coaster");
            Console.WriteLine("                gss (getstationstate) - Query state of first coaster's first station");
            Console.WriteLine(
                "                setsmmon (setstationmanualmodeon) - Enable manual mode of first coaster's first station");
            Console.WriteLine(
                "                setsmmoff (setstationmanualmodeoff) - Disable manual mode of first coaster's first station");
            Console.WriteLine("                d (dispatch) - Dispatch train in first coaster's first station");
            Console.WriteLine("                sgc (stationgatesclose) - Close gates in first coaster's first station");
            Console.WriteLine("                sgo (stationgatesopen) - Open gates in first coaster's first station");
            Console.WriteLine(
                "                shc (stationharnessclose) - Close harnesses in first coaster's first station");
            Console.WriteLine(
                "                sho (stationharnessopen) - Open harnesses in first coaster's first station");
            Console.WriteLine(
                "                spr (stationplatformraise) - Raise platform in first coaster's first station");
            Console.WriteLine(
                "                spl (stationplatformlower) - Lower platform in first coaster's first station");
            Console.WriteLine(
                "                sfl (stationflyercarlock) - Lock flyer car in first coaster's first station");
            Console.WriteLine(
                "                sfu (stationflyercarunlock) - Unlock flyer car in first coaster's first station");
            Console.WriteLine(
                "                loadpark - Load a park in default start mode (file path is hardcoded, see sourcecode)");
            Console.WriteLine(
                "                loadparkpaused - Load a park in paused start mode (file path is hardcoded, see sourcecode)");
            Console.WriteLine("                closepark - Closes current park");
            Console.WriteLine("                resetpark - Restarts current park in paused mode");
            Console.WriteLine(
                "                changeseat - Selects the second seat (seat index is hardcoded, see sourcecode");
            Console.WriteLine("                pause - Activates pause");
            Console.WriteLine("                unpause - Deactivates pause");
            Console.WriteLine("                quitserver - Request the server to quit");
            Console.WriteLine("                attractionmode - Enables attraction mode");
            Console.WriteLine("                recentervr - Recenter VR");
            Console.WriteLine(
                "                setcustomview - set a custom view (position and direction is hardcoded, see sourcecode");
        }

        /**
       * Receive a message from server
       */
        private static byte[] readMessage(BinaryReader input)
        {
            int prefix = input.Read();
            if (prefix != (int) 'N')
            {
                if (prefix != -1)
                {
                    throw new Exception("Invalid message received");
                }
                else
                {
                    throw new Exception("No data from server");
                }
            }

            int b1 = input.Read();
            if (b1 == -1)
            {
                throw new Exception("No data from server");
            }

            int b2 = input.Read();
            if (b2 == -1)
            {
                throw new Exception("No data from server");
            }

            int b3 = input.Read();
            if (b3 == -1)
            {
                throw new Exception("No data from server");
            }

            int b4 = input.Read();
            if (b4 == -1)
            {
                throw new Exception("No data from server");
            }

            int b5 = input.Read();
            if (b5 == -1)
            {
                throw new Exception("No data from server");
            }

            int b6 = input.Read();
            if (b6 == -1)
            {
                throw new Exception("No data from server");
            }

            int b7 = input.Read();
            if (b7 == -1)
            {
                throw new Exception("No data from server");
            }

            int b8 = input.Read();
            if (b8 == -1)
            {
                throw new Exception("No data from server");
            }

            int extraSize = decodeUShort16((byte) b7, (byte) b8);

            byte[] bytes = new byte[10 + extraSize];

            bytes[0] = (byte) prefix;
            bytes[1] = (byte) b1;
            bytes[2] = (byte) b2;
            bytes[3] = (byte) b3;
            bytes[4] = (byte) b4;
            bytes[5] = (byte) b5;
            bytes[6] = (byte) b6;
            bytes[7] = (byte) b7;
            bytes[8] = (byte) b8;

            for (int i = 0; i < extraSize; ++i)
            {
                int b = input.Read();
                if (b == -1)
                {
                    throw new Exception("No data from server");
                }

                bytes[9 + i] = (byte) b;
            }

            int postfix = input.Read();
            if (postfix != (int) 'L')
            {
                if (postfix != -1)
                {
                    throw new Exception("Invalid message received");
                }
                else
                {
                    throw new Exception("No data from server");
                }
            }

            bytes[9 + extraSize] = (byte) postfix;

            return bytes;
        }

        public static double ToDegrees(double angle)
        {
            return angle * (180 / Math.PI);
        }

        /**
       * Decode a received message
       */
        private static void decodeMessage(byte[] bytes)
        {
            int len = bytes.Length;
            if (len >= 10)
            {
                int msg = decodeUShort16(bytes[1], bytes[2]);
                int requestId = decodeInt32(bytes, 3);
                int size = decodeUShort16(bytes[7], bytes[8]);
                if (size + 10 == len)
                {
                    Console.Write("Server replied to request " + requestId + ": ");
                    switch (msg)
                    {
                        case N_MSG_IDLE:
                            Console.WriteLine("Idle");
                            break;
                        case N_MSG_OK:
                            Console.WriteLine("Ok");
                            break;
                        case N_MSG_ERROR:
                        {
                            Console.WriteLine("Error: " + decodeString(bytes, c_nExtraSizeOffset, size));
                        }
                            break;
                        case N_MSG_STRING:
                        {
                            Console.WriteLine("String: " + decodeString(bytes, c_nExtraSizeOffset, size));
                        }
                            break;
                        case N_MSG_VERSION:
                            if (size == 4)
                            {
                                Console.WriteLine("Version: " + bytes[c_nExtraSizeOffset] + "." +
                                                  bytes[c_nExtraSizeOffset + 1] + "." + bytes[c_nExtraSizeOffset + 2] +
                                                  "." + bytes[c_nExtraSizeOffset + 3]);
                            }

                            break;
                        case N_MSG_TELEMETRY:
                            if (size == 76)
                            {
                                int state = decodeInt32(bytes, c_nExtraSizeOffset);

                                int frameNo = decodeInt32(bytes, c_nExtraSizeOffset + 4);

                                bool inPlay = (state & 1) != 0;
                                bool onboard = (state & 2) != 0;
                                bool paused = (state & 4) != 0;

                                int viewMode = decodeInt32(bytes, c_nExtraSizeOffset + 8);
                                int coasterIndex = decodeInt32(bytes, c_nExtraSizeOffset + 12);
                                int coasterStyleId = decodeInt32(bytes, c_nExtraSizeOffset + 16);
                                int currentTrain = decodeInt32(bytes, c_nExtraSizeOffset + 20);
                                int currentCar = decodeInt32(bytes, c_nExtraSizeOffset + 24);
                                int currentSeat = decodeInt32(bytes, c_nExtraSizeOffset + 28);
                                float speed = decodeFloat(bytes, c_nExtraSizeOffset + 32);

                                Quaternion quat = new Quaternion();

                                float posx = decodeFloat(bytes, c_nExtraSizeOffset + 36);
                                float posy = decodeFloat(bytes, c_nExtraSizeOffset + 40);
                                float posz = decodeFloat(bytes, c_nExtraSizeOffset + 44);

                                quat.x = decodeFloat(bytes, c_nExtraSizeOffset + 48);
                                quat.y = decodeFloat(bytes, c_nExtraSizeOffset + 52);
                                quat.z = decodeFloat(bytes, c_nExtraSizeOffset + 56);
                                quat.w = decodeFloat(bytes, c_nExtraSizeOffset + 60);

                                float gforcex = decodeFloat(bytes, c_nExtraSizeOffset + 64);
                                float gforcey = decodeFloat(bytes, c_nExtraSizeOffset + 68);
                                float gforcez = decodeFloat(bytes, c_nExtraSizeOffset + 72);

                                double pitch = ToDegrees(quat.toPitchFromYUp());
                                double yaw = ToDegrees(quat.toYawFromYUp());
                                double roll = ToDegrees(quat.toRollFromYUp());

                                Console.WriteLine("Telemetry:");

                                Console.Write("  State: " + state);
                                if (inPlay)
                                {
                                    Console.Write(" (Play Mode)");
                                }

                                if (onboard)
                                {
                                    Console.Write(" (Onboard)");
                                }

                                if (paused)
                                {
                                    Console.Write(" (Paused)");
                                }

                                Console.WriteLine("");

                                Console.WriteLine("  Frame Number: " + frameNo);

                                Console.Write("  View Mode: " + viewMode);
                                if (viewMode == 1)
                                {
                                    Console.Write(" (Ride View)");
                                }

                                Console.WriteLine("");

                                Console.WriteLine("  Coaster Index: " + coasterIndex);
                                Console.WriteLine("  Coaster Style Id: " + coasterStyleId);
                                Console.WriteLine("  Current Train Index: " + currentTrain);
                                Console.WriteLine("  Current Car Index: " + currentCar);
                                Console.WriteLine("  Current Seat Index: " + currentSeat);
                                Console.WriteLine("  Speed: " + speed + "m/s");
                                Console.WriteLine("  Position: " + posx + " " + posy + " " + posz);
                                Console.WriteLine("  Pitch: " + pitch + "deg");
                                Console.WriteLine("  Yaw: " + yaw + "deg");
                                Console.WriteLine("  Roll: " + roll + "deg");
                                Console.WriteLine("  G-forces: " + gforcex + " " + gforcey + " " + gforcez);
                            }

                            break;
                        case N_MSG_INT_VALUE:
                            if (size == 4)
                            {
                                Console.WriteLine("Int value: " + decodeInt32(bytes, c_nExtraSizeOffset));
                            }

                            break;
                        case N_MSG_INT_VALUE_PAIR:
                            if (size == 8)
                            {
                                Console.WriteLine("Int value pair: " + decodeInt32(bytes, c_nExtraSizeOffset) + ", " +
                                                  decodeInt32(bytes, c_nExtraSizeOffset + 4));
                            }

                            break;
                        case N_MSG_STATION_STATE:
                            if (size == 4)
                            {
                                int nState = decodeInt32(bytes, c_nExtraSizeOffset);

                                bool bEStop = (nState & (1 << 0)) != 0;
                                bool bManualDispatch = (nState & (1 << 1)) != 0;
                                bool bCanDispatch = (nState & (1 << 2)) != 0;
                                bool bCanCloseGates = (nState & (1 << 3)) != 0;
                                bool bCanOpenGates = (nState & (1 << 4)) != 0;
                                bool bCanCloseHarness = (nState & (1 << 5)) != 0;
                                bool bCanOpenHarness = (nState & (1 << 6)) != 0;
                                bool bCanRaisePlatform = (nState & (1 << 7)) != 0;
                                bool bCanLowerPlatform = (nState & (1 << 8)) != 0;
                                bool bCanLockFlyerCar = (nState & (1 << 9)) != 0;
                                bool bCanUnlockFlyerCar = (nState & (1 << 10)) != 0;
                                bool bTrainInStation = (nState & (1 << 11)) != 0;
                                bool bTrainInStationIsCurrentTrain = (nState & (1 << 12)) != 0;

                                Console.Write("Station state: ");
                                Console.WriteLine("    E-Stop: " + bEStop);
                                Console.WriteLine("    Manual Dispatch: " + bManualDispatch);
                                Console.WriteLine("    Can Dispatch: " + bCanDispatch);
                                Console.WriteLine("    Can Close Gates: " + bCanCloseGates);
                                Console.WriteLine("    Can Open Gates: " + bCanOpenGates);
                                Console.WriteLine("    Can Close Harness: " + bCanCloseHarness);
                                Console.WriteLine("    Can Open Harness: " + bCanOpenHarness);
                                Console.WriteLine("    Can Raise Platform: " + bCanRaisePlatform);
                                Console.WriteLine("    Can Lower Platform: " + bCanLowerPlatform);
                                Console.WriteLine("    Can Lock Flyer Car: " + bCanLockFlyerCar);
                                Console.WriteLine("    Can Unlock Flyer Car: " + bCanUnlockFlyerCar);
                                Console.WriteLine("    Train in Station: " + bTrainInStation);
                                Console.WriteLine(
                                    "    Train in Station is Current Train: " + bTrainInStationIsCurrentTrain);
                            }

                            break;
                        default:
                            Console.WriteLine("Unknown message");
                            break;
                    }
                }
            }
        }

        /////

        /**
       * Helper class to decode rotation quaternion into pitch/yaw/roll
       */
        class Quaternion
        {
            public double x;
            public double y;
            public double z;
            public double w;

            public double toPitchFromYUp()
            {
                double vx = 2 * (x * y + w * y);
                double vy = 2 * (w * x - y * z);
                double vz = 1.0 - 2 * (x * x + y * y);

                return Math.Atan2(vy, Math.Sqrt(vx * vx + vz * vz));
            }

            public double toYawFromYUp()
            {
                return Math.Atan2(2 * (x * y + w * y), 1.0 - 2 * (x * x + y * y));
            }

            public double toRollFromYUp()
            {
                return Math.Atan2(2 * (x * y + w * z), 1.0 - 2 * (x * x + z * z));
            }

        }
    }
}