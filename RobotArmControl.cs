using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Modbus;
using System.Threading;
using System.IO.Ports;


//介面是Robotic_Arm.cs
//使用的functioin 用RobotArmControl來做
//將介面程式分開

namespace RobotArmControl
{

    public class cModBusData
    {
        public const ushort DEF_MAX_AXIS = 7;
        // public const ushort REG_INPUT_START = 3000;
        // public const ushort REG_INPUT_NREGS = 15;
        public const ushort REG_HOLDING_START = 4000;
        public const ushort REG_HOLDING_NREGS = 28;


        enum eErrorSate
        {
            RECEIVE_NULL = 1,
            RECEIVE_LENGTH,
            WRONG_POS,
            SINGULAR_POINT
        }
        enum eOPMode
        {
            JOG,
            P2P,
            SEWING,
            LINE
        }



        //==
        //Modbus struct
        //==
        //Input register  0~14 
        public UInt16[] PosVal = new UInt16[DEF_MAX_AXIS];
        public UInt16[] VelValue = new UInt16[DEF_MAX_AXIS];
        public UInt16 Err_State = 0;

        //Holding register  0~5
        public Int16 TargetPosX;
        public Int16 TargetPosY;
        public Int16 TargetPosZ;
        public Int16 OPMode;       //P2P
        public float SpeedRatio; //0~1
        public UInt16[] TarPos = new UInt16[DEF_MAX_AXIS];

        ModbusMasterSerial mm;

        //Master Thread
        Thread Th_ModBusMaster;
        bool bSwModBusMaster;

        ManualResetEvent Th_mb_pauseEvent;
        public bool Th_mb_ReadDone = false;
        //construct
        public cModBusData()
        {
            // Crete instance of modbus serial RTU (replace COMx with a free serial port - ex. COM5)
            mm = new ModbusMasterSerial(ModbusSerialType.RTU, "COM8", 115200, 8, Parity.None, StopBits.One, Handshake.None);
            // Exec the connection
            mm.Connect();

            Th_mb_pauseEvent = new ManualResetEvent(true);

            //intial modbus
            //ModBusData.ModbusRTUMaster_Initial();


        }

        public void StartModbusMasterThread()
        {
            Th_ModBusMaster = new Thread(ModbusMasterThreadFun);//會跳錯
            bSwModBusMaster = true;
            Th_ModBusMaster.Start();
        }

        public void PauseModbusMasterThread()
        {
            Th_mb_pauseEvent.Reset();


        }

        public void ResumeModbusMasterThread()
        {
            Th_mb_pauseEvent.Set();
        }

        public void EndModbusMasterThread()
        {
            bSwModBusMaster = false;
        }


        //public void ModbusRTUMaster_Initial()
        //{
        //    // Crete instance of modbus serial RTU (replace COMx with a free serial port - ex. COM5)
        //    mm = new ModbusMasterSerial(ModbusSerialType.RTU, "COM8", 115200, 8, Parity.None, StopBits.One, Handshake.None);
        //    // Exec the connection
        //    mm.Connect();
        //}
        public void ModbusMasterThreadFun()
        {
            while (bSwModBusMaster)
            {
                Th_mb_ReadDone = false;
                Th_mb_pauseEvent.WaitOne(Timeout.Infinite);  //一開始有signal 可以通過，如果reset之後會卡在這邊

                ModbusRTUMaster_ReadRegister(10);
                Th_mb_ReadDone = true;
                Thread.Sleep(100);


            }

        }
        public float TwoUshortToFloat(ushort low, ushort high)//把兩個16bit 組合成float
        {
            float result = 0;
            byte[] byte_array = { (byte)(low & 0xff), (byte)(low >> 8), (byte)(high & 0xff), (byte)(high >> 8) };
            result = BitConverter.ToSingle(byte_array, 0);

            return result;
        }

        public void ModbusRTUMaster_ReadRegister(byte unit_id)
        {
            //problem 
            //1.ReadHoldingRegisters after ReadInputRegisters,then the receive data will change

            //===================//
            //==Read Input resister
            //===================//
            //ushort[] InputReg_array = mm.ReadInputRegisters(unit_id, REG_INPUT_START, REG_INPUT_NREGS);
            //if (InputReg_array == null)
            //{
            //    Err_State = (UInt16)eErrorSate.RECEIVE_NULL;
            //    return;
            //}
            //else if (InputReg_array.Length != REG_INPUT_NREGS)//stanley
            //{
            //    Err_State = (UInt16)eErrorSate.RECEIVE_LENGTH;
            //    return;
            //}

            //int i = 0;
            //for (i = 0; i < DEF_MAX_AXIS; i++)
            //{
            //    PosVal[i] = InputReg_array[i];
            //    VelValue[i] = InputReg_array[i + DEF_MAX_AXIS];
            //}

            //Err_State = InputReg_array[7 + 7];
            //TargetPosX = Convert.ToInt16(num_array[7 + 7 + 1]);
            //TargetPosY = Convert.ToInt16(num_array[7 + 7 + 2]);
            //TargetPosZ = Convert.ToInt16(num_array[7 + 7 + 3]);
            //OPMode = Convert.ToInt16(num_array[7 + 7 + 4]);
            //SpeedRatio = Convert.ToInt16(num_array[7 + 7 + 5]);

            //Console.Write("addr999=0x" + num_array[0].ToString("x") + "  " +
            //                "addr1000=0x" + num_array[1].ToString("x") + "  " +
            //                "addr1001=0x" + num_array[2].ToString("x") + "  " +
            //                "addr1002=0x" + num_array[3].ToString("x") + Environment.NewLine);
            //Thread.Sleep(100);


            //========================//
            //==Read Holding resister=//
            //========================//
            ushort[] HoldReg_array = mm.ReadHoldingRegisters(unit_id, REG_HOLDING_START, REG_HOLDING_NREGS);  //ReadInputRegisters 執行完直接再Read會出錯
            if (HoldReg_array == null)
            {
                Err_State = (UInt16)eErrorSate.RECEIVE_NULL;
                return;
            }
            else if (HoldReg_array.Length != REG_HOLDING_NREGS) //stanley
            {
                Err_State = (UInt16)eErrorSate.RECEIVE_LENGTH;
                return;
            }

            TargetPosX = Convert.ToInt16(HoldReg_array[0]);
            TargetPosY = Convert.ToInt16(HoldReg_array[1]);
            TargetPosZ = Convert.ToInt16(HoldReg_array[2]);
            OPMode = Convert.ToInt16(HoldReg_array[3]);
            SpeedRatio = TwoUshortToFloat(HoldReg_array[4], HoldReg_array[5]);

            for (byte i = 0; i < DEF_MAX_AXIS; i++)
            {
                TarPos[i] = HoldReg_array[6 + i];
                PosVal[i] = HoldReg_array[13 + i];
                VelValue[i] = HoldReg_array[20 + i];
            }

            Err_State = HoldReg_array[27];

        }


        public void AxisMoveToPos(byte unit_id, ushort axis, ushort pos)
        {
            if (axis > DEF_MAX_AXIS)
                return;

            ushort address = Convert.ToUInt16(4005 + axis);
            //mm.WriteSingleRegister(unit_id, address, pos);
            mm.WriteSingleRegister(unit_id, address, pos);//

        }

        public void TestThreadFun()
        {
            int abc = 0;
            abc++;
        }

    }

}
