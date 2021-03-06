﻿using System;
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
            JOG=0,
            P2P,
            SEWING,
            LINE,
            IDLE
        }

        
        enum eMbData
        {
	        DEF_INX_TARGET_POSX=0,
	        DEF_INX_TARGET_POSY,
	        DEF_INX_TARGET_POSZ,
	        DEF_INX_OPMODE,
	        DEF_INX_SPPED_RATIO_L,
	        DEF_INX_SPPED_RATIO_H,

	        DEF_INX_TARPOS1=6,
	        DEF_INX_TARPOS2,
	        DEF_INX_TARPOS3,
	        DEF_INX_TARPOS4,
	        DEF_INX_TARPOS5,
	        DEF_INX_TARPOS6,
	        DEF_INX_TARPOS7,

	        DEF_INX_TARVEL1=13,
            DEF_INX_TARVEL2,
            DEF_INX_TARVEL3,
            DEF_INX_TARVEL4,
            DEF_INX_TARVEL5,
            DEF_INX_TARVEL6,
            DEF_INX_TARVEL7,

            DEF_INX_POSVAL1 = 20,
            DEF_INX_POSVAL2,
            DEF_INX_POSVAL3,
            DEF_INX_POSVAL4,
            DEF_INX_POSVAL5,
            DEF_INX_POSVAL6,
            DEF_INX_POSVAL7,

	        DEF_INX_ERR_STATE,
	        DEF_INX_STATE
        };

        //==
        //Modbus struct
        //==
        //Holding register  0~5
        public Int16 TargetPosX;
        public Int16 TargetPosY;
        public Int16 TargetPosZ;
        public Int16 OPMode;       //P2P
        public float SpeedRatio; //0~1
        public UInt16[] TarPos = new UInt16[DEF_MAX_AXIS];
        public UInt16[] TarVel = new UInt16[DEF_MAX_AXIS];
        public UInt16[] PosVal = new UInt16[DEF_MAX_AXIS];
        public UInt16 Err_State = 0;


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
            mm = new ModbusMasterSerial(ModbusSerialType.RTU, "COM8", 460800, 8, Parity.None, StopBits.One, Handshake.None);
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
                //TarPos[i] = HoldReg_array[6 + i];
                PosVal[i] = HoldReg_array[Convert.ToUInt16(eMbData.DEF_INX_POSVAL1 + i)];
            }

            Err_State = HoldReg_array[27];

        }


        public void Action_Jog(byte unit_id, ushort axis, ushort pos)
        {
            if (axis > DEF_MAX_AXIS)
                return;

            ushort address = Convert.ToUInt16(REG_HOLDING_START + eMbData.DEF_INX_TARPOS1 + axis-1);
            mm.WriteSingleRegister(unit_id, address, pos);
            
            Thread.Sleep(10);

            address = Convert.ToUInt16(REG_HOLDING_START + eMbData.DEF_INX_OPMODE);
            mm.WriteSingleRegister(unit_id, address, Convert.ToUInt16(eOPMode.JOG));
        }

        public void Action_Multi_Jog(byte unit_id, ushort[] pos,ushort[] vel) //一次下7軸
        {
            
            ushort address = Convert.ToUInt16(REG_HOLDING_START + eMbData.DEF_INX_TARPOS1);

            mm.WriteMultipleRegisters(unit_id, address,pos);
            Thread.Sleep(10);

            address = Convert.ToUInt16(REG_HOLDING_START + eMbData.DEF_INX_TARVEL1);
            mm.WriteMultipleRegisters(unit_id, address, vel);
            Thread.Sleep(10);

            address = Convert.ToUInt16(REG_HOLDING_START + eMbData.DEF_INX_OPMODE);
            mm.WriteSingleRegister(unit_id, address, Convert.ToUInt16(eOPMode.JOG));
        }


        public void Action_P2P(byte unit_id, ushort x, ushort y, ushort z)
        {
            ushort []values = {x,y,z};
            ushort address = Convert.ToUInt16(REG_HOLDING_START + eMbData.DEF_INX_TARGET_POSX);
             
            mm.WriteMultipleRegisters(unit_id, address, values);
            

            Thread.Sleep(10);

            address = Convert.ToUInt16(REG_HOLDING_START + eMbData.DEF_INX_OPMODE);
            mm.WriteSingleRegister(unit_id, address, Convert.ToUInt16(eOPMode.P2P));
        }

        public void TestThreadFun()
        {
            int abc = 0;
            abc++;
        }

    }

}
