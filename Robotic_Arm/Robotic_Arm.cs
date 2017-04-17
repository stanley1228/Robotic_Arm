//1.缺alpha beta gamma

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Modbus;
using System.IO.Ports;
using System.Threading;


   
//public  void Test_ModbusRTUMaster()
//    {
//        byte unit_id = 10;
//        // Crete instance of modbus serial RTU (replace COMx with a free serial port - ex. COM5)
//        ModbusMasterSerial mm = new ModbusMasterSerial(ModbusSerialType.RTU, "COM8", 115200, 8, Parity.None, StopBits.One, Handshake.None);
//        // Exec the connection
//        mm.Connect();

    
//        while (true)
//        {

//            ushort[] num_array = mm.ReadInputRegisters(unit_id, 999, 4);
//            if (num_array == null)
//                Console.Write("read failed" + Environment.NewLine);
//            else
//            {
//                PosVal[0] = num_array[0];
//                Console.Write("addr999=0x" + num_array[0].ToString("x") + "  " +
//                                "addr1000=0x" + num_array[1].ToString("x") + "  " +
//                                "addr1001=0x" + num_array[2].ToString("x") + "  " +
//                                "addr1002=0x" + num_array[3].ToString("x") + Environment.NewLine);

//            }
//            //Console.Write(
//            //    "---------------------- READING ----------------------" + Environment.NewLine +
//            //    //"Holding register n.1  : " + mm.ReadHoldingRegisters(unit_id, 0, 1).First().ToString("D5") + Environment.NewLine +
//            //    "Input register   n.41 : " + mm.ReadInputRegisters(unit_id, 999, 3).First().ToString("x") + Environment.NewLine);
//            //"Coil register    n 23 : " + mm.ReadCoils(unit_id, 22, 1).First().ToString() + Environment.NewLine +
//            //    "---------------------- WRITING ----------------------" + Environment.NewLine);
//            // mm.WriteSingleRegister(unit_id, 4, (ushort)rnd.Next(ushort.MinValue, ushort.MaxValue));
//            // Console.WriteLine(
//            //     "Holding register n.5  : " + mm.ReadHoldingRegisters(unit_id, 4, 1).First().ToString("D5") + Environment.NewLine);
//            // mm.WriteSingleCoil(unit_id, 2, Convert.ToBoolean(rnd.Next(0, 1)));
//            // Console.WriteLine(
//            //     "Coil register    n.3  : " + mm.ReadCoils(unit_id, 2, 1).First().ToString() + Environment.NewLine);
//            // Exec the cicle each 2 seconds
//            Thread.Sleep(100);
//        }
//    }


namespace Robotic_Arm
{
    

    public partial class Robotic_Arm : Form
    {
        
        RobotArmControl.cModBusData ModBusData = new RobotArmControl.cModBusData();

       

        public Robotic_Arm()
        {
            InitializeComponent();
        }



        private void Robotix_Arm_Load(object sender, EventArgs e)
        {
            txb_TargetPosX.Text="60";
            txb_TargetPosY.Text="0";
            txb_TargetPosZ.Text="0";
            txb_SpeedRatio.Text = "0.5";

            //Add OP mode combox item
            String[] strmode=new String[]{"JOG","P2P","Line","Sewing"};
            foreach (String str1 in strmode)
                cbx_OPMode.Items.Add(str1);

            cbx_OPMode.SelectedIndex = 0;


            //Add JOG axis combox item
            String[] AxisSel=new String[]{"Axis1","Axis2","Axis3","Axis4","Axis5","Axis6","Axis7"};
            foreach (String str2 in AxisSel)
                cbx_JOGAXIS.Items.Add(str2);
            cbx_JOGAXIS.SelectedIndex = 0;

            textBox1.Text = "1";

           
            //Timer for display on UI
            System.Windows.Forms.Timer Dis_Timer = new System.Windows.Forms.Timer();
            Dis_Timer.Interval = (500); // 45 mins
            Dis_Timer.Tick += new EventHandler(Dis_Timer_Fun);
            Dis_Timer.Start();

            //Initail Target pos Value
            TextBox[] tar_pos = new TextBox[] { textBox16, textBox17, textBox18, textBox19, textBox20, textBox21, textBox22 };
            for (int i = 0; i < RobotArmControl.cModBusData.DEF_MAX_AXIS; i++)
            {
                //txb_Vel[i].Text = String.Format("0x{0:X}", ModBusData.VelValue[i]);
                tar_pos[i].Text = String.Format("{0}", 0);
            }
            tar_pos[4].Text = String.Format("{0}", 100);
            tar_pos[5].Text = String.Format("{0}", 200);
            tar_pos[6].Text = String.Format("{0}", 300);


            //Initail Target pos Value
            TextBox[] text_tar_vel = new TextBox[] { textBox8, textBox9, textBox10, textBox11, textBox12, textBox13, textBox14 };
            for (int i = 0; i < RobotArmControl.cModBusData.DEF_MAX_AXIS; i++)               
                text_tar_vel[i].Text = String.Format("{0}", 50);
         



            //start Modbus master thread
            //ModBusData.StartModbusMasterThread();
           

        }

        private void BTN_SEND_Click(object sender, EventArgs e)
        {
            byte unit_id=10;
            ushort axis = 0;
            ushort pos = 0;
            if (cbx_OPMode.Text == "JOG")
            {
                //=============
                //寫入要移動的軸
                //=============
                axis = Convert.ToUInt16(cbx_JOGAXIS.SelectedIndex + 1);
                pos = Convert.ToUInt16(textBox15.Text);

               // ModBusData.PauseModbusMasterThread(); 
                int count = 0;
                while ((ModBusData.Th_mb_ReadDone == true) && (count<500))
                {
                    count++;
                    Thread.Sleep(1);
                }

                if (count < 500)
                {
                    ModBusData.Action_Jog(unit_id, axis, pos); //可能在送的時候會同時收會發生相撞
                    Thread.Sleep(10);//stanley沒加delay會無法寫
                }
                //ModBusData.ResumeModbusMasterThread();

            }
            else if (cbx_OPMode.Text == "P2P")
            {
                short x = 0, y = 0, z = 0;
                x = Convert.ToInt16(txb_TargetPosX.Text);
                y = Convert.ToInt16(txb_TargetPosY.Text);
                z = Convert.ToInt16(txb_TargetPosZ.Text);

                //ModBusData.PauseModbusMasterThread();
                int count = 0;
                while ((ModBusData.Th_mb_ReadDone == true) && (count < 500))
                {
                    count++;
                    Thread.Sleep(1);
                }

                if (count < 500)
                {
                    ModBusData.Action_P2P(unit_id, (ushort)x, (ushort)y,(ushort)z); //可能在送的時候會同時收會發生相撞
                    Thread.Sleep(10);//stanley沒加delay會無法寫
                }
            }
                
        }
        private void Dis_Timer_Fun(object sender, EventArgs e)
        {

            //ModBusData.ModbusRTUMaster_ReadRegister(id);

            //current pos
            TextBox[] txb_PosVal = new TextBox[] { textBox1, textBox2, textBox3, textBox4, textBox5, textBox6, textBox7};
            for (int i = 0; i < RobotArmControl.cModBusData.DEF_MAX_AXIS; i++)
            {
                //txb_PosVal[i].Text = String.Format("0x{0:X}", ModBusData.PosVal[i]); 
                txb_PosVal[i].Text=String.Format("{0}",ModBusData.PosVal[i]);
            }
            //Error State
            //txB_ERRSTATE.Text = String.Format("0x{0:X}", ModBusData.Err_State);
            txB_ERRSTATE.Text = String.Format("{0}", ModBusData.Err_State);
            

            //TextBox[] tar_pos = new TextBox[] { textBox16, textBox17, textBox18, textBox19, textBox20, textBox21, textBox22 };
            //for (int i = 0; i < RobotArmControl.cModBusData.DEF_MAX_AXIS; i++)
            //{
            //    //txb_Vel[i].Text = String.Format("0x{0:X}", ModBusData.VelValue[i]);
            //    tar_pos[i].Text = String.Format("{0}", ModBusData.TarPos[i]);
            //}



            //txb_TargetPosX.Text = ModBusData.TargetPosX.ToString();
            //txb_TargetPosY.Text = ModBusData.TargetPosY.ToString();
            //txb_TargetPosZ.Text = ModBusData.TargetPosZ.ToString();
            //cbx_OPMode.SelectedIndex = ModBusData.OPMode;
            //txb_SpeedRatio.Text = ModBusData.SpeedRatio.ToString();

            //txb_TargetPosX.Text = String.Format("{0}", ModBusData.TargetPosX);
            //txb_TargetPosY.Text = String.Format("{0}", ModBusData.TargetPosY);
            //txb_TargetPosZ.Text = String.Format("{0}", ModBusData.TargetPosZ);
            //cbx_OPMode.SelectedIndex = ModBusData.OPMode;
            txb_SpeedRatio.Text = String.Format("{0:0.000}", ModBusData.SpeedRatio);
        }

        private void threadtest()
        {
            int a = 1;
            a++;
            a = 10;

        }

        private void Robotic_Arm_FormClosed(object sender, FormClosedEventArgs e)
        {
            ModBusData.EndModbusMasterThread();
        }

        private void BTN_STATUS_Click(object sender, EventArgs e)
        {
           ModBusData.ModbusRTUMaster_ReadRegister(10);
        }

        private void BTN_MULTI_JOG_Click(object sender, EventArgs e)
        {
            ushort[] tar_pos = new ushort[RobotArmControl.cModBusData.DEF_MAX_AXIS];
            ushort[] tar_vel = new ushort[RobotArmControl.cModBusData.DEF_MAX_AXIS];

            TextBox[] text_tar_pos = new TextBox[] { textBox16, textBox17, textBox18, textBox19, textBox20, textBox21, textBox22 };
            for (int i = 0; i < RobotArmControl.cModBusData.DEF_MAX_AXIS; i++)
            {
                tar_pos[i]= Convert.ToUInt16(text_tar_pos[i].Text);       
            }

            TextBox[] text_tar_vel = new TextBox[] { textBox8, textBox9, textBox10, textBox11, textBox12, textBox13, textBox14 };
            for (int i = 0; i < RobotArmControl.cModBusData.DEF_MAX_AXIS; i++)
            {
                tar_vel[i] = Convert.ToUInt16(text_tar_vel[i].Text);
            }



            ModBusData.Action_Multi_Jog(10, tar_pos, tar_vel); //一次下7軸
        }

 


    }

}

