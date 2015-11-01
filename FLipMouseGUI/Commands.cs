﻿
/* 
 
  Supported AT-commands:  
  (sent via serial interface, 115200 baud, using spaces between parameters.  Enter (<cr>, ASCII-code 0x0d) finishes a command)
   
         AT                returns "OK"
         AT ID             returns identification string (e.g. "FLipMouse V2.0")
         AT BM <uint>      puts button into programming mode (e.g. "AT BM 2" -> next AT-command defines the new function for button 2)
                           for the FLipmouse, there are 11 buttons available (3 physical buttons, 8 virtual functions): 

                             1: internal button1 / Special UP
                             2: external button2 / Special LEFT 
                             3: external button3 / Special RIGHT 
                             4: alternative UP 
                             5: alternative DOWN 
                             6: alternative LEFT 
                             7: alternative RIGHT
                             8: SIP (pressure lower than sip threshold)
                             9: Special SIP
                             10: PUFF (pressure bigger than puff threshold)
                             11: Special PUFF

   USB HID commands:
      
         AT CL             click left mouse button  
         AT CR             click right mouse button  
         AT CM             click middle mouse button  
         AT CD             click double with left mouse button

         AT PL             press/hold the left mouse button  
         AT PR             press/hold the right mouse button
         AT PM             press/hold the middle mouse button 
  
         AT RL             release the left mouse button  
         AT RR             release the right mouse button
         AT RM             release the middle mouse button 
          
         AT WU             move mouse wheel up  
         AT WD             move mouse wheel down  
         AT WS <uint>      set mouse wheel stepsize (e.g. "AT WS 3" sets the wheel stepsize to 3 rows)
   
         AT MX <int>       move mouse in x direction (e.g. "AT MX 4" moves cursor 4 pixels to the right)  
         AT MY <int>       move mouse in y direction (e.g. "AT MY -10" moves cursor 10 pixels up)  

         AT KW <string>    keyboard write string (e.g." AT KW Hello!" writes "Hello!")    
         AT KP <string>    key press: press/hold keys identifier 
                           (e.g. "AT KP KEY_UP" presses the "Cursor-Up" key, "AT KP KEY_CTRL KEY_ALT KEY_DELETE" presses all three keys)
                           for a list of supported key idientifier strings see below ! 
                            
         AT KR <string>    key release: releases all keys identified in the string    
         AT RA             release all: releases all currently pressed keys and buttons    
          
   Housekeeping commands:

         AT SA <string>  save settings and current button modes to next free eeprom slot under given name (e.g. AT SAVE mouse1)
         AT LO <string>  load button modes from eeprom slot (e.g. AT LOAD mouse1 -> loads profile named "mouse1")
         AT LA           load all slots (displays names and settings of all stored slots) 
         AT LI           list all saved mode names 
         AT NE           next mode will be loaded (wrap around after last slot)
         AT DE           delete EEPROM content (delete all stored slots)
         AT NC           no command (idle operation)
          
   FLipMouse-specific settings and commands:

         AT MM <uint>    mouse mode: cursor on (uint==1) or alternative functions on (uint==0)
         AT SW           switch between mouse cursor and alternative functions
         AT SR           start reporting raw values (5 sensor values, starting with "VALUES:") 
         AT ER           end reporting raw values
         AT CA           calibration of zeropoint
         AT AX <uint>    acceleration x-axis  (0-100)
         AT AY <uint>    acceleration y-axis  (0-100)
         AT DX <uint>    deadzone x-axis  (0-1000)
         AT DY <uint>    deadzone y-axis  (0-1000)
         AT TS <uint>    treshold for sip action  (0-512)
         AT TP <uint>    treshold for puff action (512-1023)
         AT SM <uint>    treshold for special mode (512-1023)
         AT HM <uint>    treshold for hold mode (0-512)
         AT GU <uint>    gain for up sensor (0-100)
         AT GD <uint>    gain for down sensor (0-100)
         AT GL <uint>    gain for left sensor (0-100)
         AT GR <uint>    gain for right sensor (0-100)
  
   Infrared-specific commands:

         AT IR <string>  record new infrared code and store it under given name (e.g. "AT IR vol_up")
         AT IP <string>  play  infrared code with given name (e.g. "AT IP vol_up")
         AT IC <string>  clear infrared code with given name (e.g. "AT ID vol_up")
         AT IL           lists all stored infrared command names

  supported key identifiers for key press command (AT KP):
 
   KEY_A   KEY_B   KEY_C   KEY_D    KEY_E   KEY_F   KEY_G   KEY_H   KEY_I   KEY_J    KEY_K    KEY_L
   KEY_M   KEY_N   KEY_O   KEY_P    KEY_Q   KEY_R   KEY_S   KEY_T   KEY_U   KEY_V    KEY_W    KEY_X 
   KEY_Y   KEY_Z   KEY_1   KEY_2    KEY_3   KEY_4   KEY_5   KEY_6   KEY_7   KEY_8    KEY_9    KEY_0
   KEY_F1  KEY_F2  KEY_F3  KEY_F4   KEY_F5  KEY_F6  KEY_F7  KEY_F8  KEY_F9  KEY_F10  KEY_F11  KEY_F12	
    
   KEY_RIGHT   KEY_LEFT       KEY_DOWN        KEY_UP      KEY_ENTER    KEY_ESC   KEY_BACKSPACE   KEY_TAB	
   KEY_HOME    KEY_PAGE_UP    KEY_PAGE_DOWN   KEY_DELETE  KEY_INSERT   KEY_END	  KEY_NUM_LOCK    KEY_SCROLL_LOCK
   KEY_SPACE   KEY_CAPS_LOCK  KEY_PAUSE       KEY_SHIFT   KEY_CTRL     KEY_ALT   KEY_RIGHT_ALT   KEY_GUI 
   KEY_RIGHT_GUI
    
*/



using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;


namespace MouseApp2
{
    public partial class FLipMouseGUI
    {
        const int PARTYPE_NONE = 0;
        const int PARTYPE_UINT = 1;
        const int PARTYPE_INT =  2;
        const int PARTYPE_STRING = 3;

        const int COMBOENTRY_YES = 1;
        const int COMBOENTRY_NO = 0;

        const int GUITYPE_STANDARD  = 0;
        const int GUITYPE_INTFIELD  = 1;
        const int GUITYPE_TEXTFIELD = 2;
        const int GUITYPE_KEYSELECT = 3;
        const int GUITYPE_SLIDER    = 4;
        const int GUITYPE_BOOLEAN   = 5;

        const string PREFIX_FLIPMOUSE_VERSION = "FLIPMOUSE ";
        const string PREFIX_REPORT_VALUES = "VALUES:";
        const string PREFIX_SLOT_NAME = "SLOT:";
        const string PREFIX_AT_COMMAND = "AT ";
        const string PREFIX_END_OF_SLOTS = "END";


        public AllCommands allCommands = new AllCommands();
        public void initCommands()
        {
            allCommands.add(new Command("AT ID", PARTYPE_NONE, "Get Id String", COMBOENTRY_NO, GUITYPE_STANDARD));
            allCommands.add(new Command("AT BM", PARTYPE_UINT, "Update Button mode", COMBOENTRY_NO, GUITYPE_STANDARD));
            allCommands.add(new Command("AT CL", PARTYPE_NONE, "Click Left Mouse Button", COMBOENTRY_YES, GUITYPE_STANDARD));
            allCommands.add(new Command("AT CR", PARTYPE_NONE, "Click Right Mouse Button", COMBOENTRY_YES, GUITYPE_STANDARD));
            allCommands.add(new Command("AT CM", PARTYPE_NONE, "Click Middle Mouse Button", COMBOENTRY_YES, GUITYPE_STANDARD));
            allCommands.add(new Command("AT CD", PARTYPE_NONE, "Double Click Left Mouse Button", COMBOENTRY_YES, GUITYPE_STANDARD));
            allCommands.add(new Command("AT PL", PARTYPE_NONE, "Hold Left Mouse Button", COMBOENTRY_YES, GUITYPE_STANDARD));
            allCommands.add(new Command("AT PR", PARTYPE_NONE, "Hold Right Mouse Button", COMBOENTRY_YES, GUITYPE_STANDARD));
            allCommands.add(new Command("AT PM", PARTYPE_NONE, "Hold Middle Mouse Button", COMBOENTRY_YES, GUITYPE_STANDARD));
            allCommands.add(new Command("AT RL", PARTYPE_NONE, "Release Left Mouse Button", COMBOENTRY_NO, GUITYPE_STANDARD));
            allCommands.add(new Command("AT RR", PARTYPE_NONE, "Release Right Mouse Button", COMBOENTRY_NO, GUITYPE_STANDARD));
            allCommands.add(new Command("AT RM", PARTYPE_NONE, "Release Middle Mouse Button", COMBOENTRY_NO, GUITYPE_STANDARD));
            allCommands.add(new Command("AT WU", PARTYPE_NONE, "Wheel Up", COMBOENTRY_YES, GUITYPE_STANDARD));
            allCommands.add(new Command("AT WD", PARTYPE_NONE, "Wheel Down", COMBOENTRY_YES, GUITYPE_STANDARD));
            allCommands.add(new Command("AT WS", PARTYPE_UINT, "Mouse Wheel Step Size", COMBOENTRY_NO, GUITYPE_STANDARD));
            allCommands.add(new Command("AT MX", PARTYPE_INT, "Move Mouse X", COMBOENTRY_YES, GUITYPE_INTFIELD));
            allCommands.add(new Command("AT MY", PARTYPE_INT, "Move Mouse Y", COMBOENTRY_YES, GUITYPE_INTFIELD));
            allCommands.add(new Command("AT KW", PARTYPE_STRING, "Write Text", COMBOENTRY_YES, GUITYPE_TEXTFIELD));
            allCommands.add(new Command("AT KP", PARTYPE_STRING, "Press Keys", COMBOENTRY_YES, GUITYPE_KEYSELECT));
            allCommands.add(new Command("AT KR", PARTYPE_STRING, "Release Keys", COMBOENTRY_NO, GUITYPE_STANDARD));
            allCommands.add(new Command("AT RA", PARTYPE_NONE, "Release All", COMBOENTRY_NO, GUITYPE_STANDARD));
            allCommands.add(new Command("AT SA", PARTYPE_STRING, "Save Slot", COMBOENTRY_NO, GUITYPE_STANDARD));
            allCommands.add(new Command("AT LO", PARTYPE_STRING, "Load Slot", COMBOENTRY_NO, GUITYPE_STANDARD));
            allCommands.add(new Command("AT LA", PARTYPE_NONE, "Load All", COMBOENTRY_NO, GUITYPE_STANDARD));
            allCommands.add(new Command("AT LI", PARTYPE_NONE, "List Slots", COMBOENTRY_NO, GUITYPE_STANDARD));
            allCommands.add(new Command("AT NE", PARTYPE_NONE, "Switch to next configuration", COMBOENTRY_YES, GUITYPE_STANDARD));
            allCommands.add(new Command("AT DE", PARTYPE_NONE, "Delete all configurations", COMBOENTRY_NO, GUITYPE_STANDARD));
            allCommands.add(new Command("AT NC", PARTYPE_NONE, "No Command", COMBOENTRY_YES, GUITYPE_STANDARD));
            allCommands.add(new Command("AT MM", PARTYPE_UINT, "Mouse Mode (1) or Alternative (0)", COMBOENTRY_NO, GUITYPE_BOOLEAN));
            allCommands.add(new Command("AT SW", PARTYPE_NONE, "Switch Mouse/Alternative", COMBOENTRY_YES, GUITYPE_STANDARD));
            allCommands.add(new Command("AT SR", PARTYPE_NONE, "Start Rawvalue reports", COMBOENTRY_NO, GUITYPE_STANDARD));
            allCommands.add(new Command("AT ER", PARTYPE_NONE, "End Rawvalue reports", COMBOENTRY_NO, GUITYPE_STANDARD));
            allCommands.add(new Command("AT CA", PARTYPE_NONE, "Calibrate Middle Position", COMBOENTRY_YES, GUITYPE_STANDARD));
            allCommands.add(new Command("AT AX", PARTYPE_UINT, "Acceleration X", COMBOENTRY_NO, GUITYPE_SLIDER));
            allCommands.add(new Command("AT AY", PARTYPE_UINT, "Acceleration Y", COMBOENTRY_NO, GUITYPE_SLIDER));
            allCommands.add(new Command("AT DX", PARTYPE_UINT, "Deadzone X", COMBOENTRY_NO, GUITYPE_SLIDER));
            allCommands.add(new Command("AT DY", PARTYPE_UINT, "Deadzone Y", COMBOENTRY_NO, GUITYPE_SLIDER));
            allCommands.add(new Command("AT TS", PARTYPE_UINT, "Theshold Sip", COMBOENTRY_NO, GUITYPE_SLIDER));
            allCommands.add(new Command("AT TP", PARTYPE_UINT, "Theshold Puff", COMBOENTRY_NO, GUITYPE_SLIDER));
            allCommands.add(new Command("AT SM", PARTYPE_UINT, "Threshold Special Mode", COMBOENTRY_NO, GUITYPE_SLIDER));
            allCommands.add(new Command("AT HM", PARTYPE_UINT, "Threshold Hold Mode", COMBOENTRY_NO, GUITYPE_SLIDER));
            allCommands.add(new Command("AT GU", PARTYPE_UINT, "Gain for Up Sensor", COMBOENTRY_NO, GUITYPE_SLIDER));
            allCommands.add(new Command("AT GD", PARTYPE_UINT, "Gain for Down Sensor", COMBOENTRY_NO, GUITYPE_SLIDER));
            allCommands.add(new Command("AT GL", PARTYPE_UINT, "Gain for Left Sensor", COMBOENTRY_NO, GUITYPE_SLIDER));
            allCommands.add(new Command("AT GR", PARTYPE_UINT, "Gain for Right Sensor", COMBOENTRY_NO, GUITYPE_SLIDER));
            allCommands.add(new Command("AT IR", PARTYPE_STRING, "Record Infrared Command", COMBOENTRY_YES, GUITYPE_TEXTFIELD));
            allCommands.add(new Command("AT IP", PARTYPE_STRING, "Play Infrared Command", COMBOENTRY_YES, GUITYPE_TEXTFIELD));
            allCommands.add(new Command("AT IC", PARTYPE_STRING, "Clear Infrared Command", COMBOENTRY_YES, GUITYPE_TEXTFIELD));
            allCommands.add(new Command("AT IL", PARTYPE_NONE, "List Infrared Commands", COMBOENTRY_YES, GUITYPE_STANDARD));

        }


        public CommandGuiLinks commandGuiLinks = new CommandGuiLinks();

        public void initCommandGuiLinks()
        {
            commandGuiLinks.add(new CommandGuiLink("AT AX", speedXBar, speedXLabel));
            commandGuiLinks.add(new CommandGuiLink("AT AY", speedYBar, speedYLabel));
            commandGuiLinks.add(new CommandGuiLink("AT DX", deadzoneXBar, deadzoneXLabel));
            commandGuiLinks.add(new CommandGuiLink("AT DY", deadzoneYBar, deadzoneYLabel));
            commandGuiLinks.add(new CommandGuiLink("AT TS", sipThresholdBar, sipThresholdLabel));
            commandGuiLinks.add(new CommandGuiLink("AT TP", puffThresholdBar, puffThresholdLabel));
            commandGuiLinks.add(new CommandGuiLink("AT SM", specialThresholdBar, specialThresholdLabel));
            commandGuiLinks.add(new CommandGuiLink("AT HM", holdThresholdBar, holdThresholdLabel));
            commandGuiLinks.add(new CommandGuiLink("AT GU", upGainBar, upGainLabel));
            commandGuiLinks.add(new CommandGuiLink("AT GD", downGainBar, downGainLabel));
            commandGuiLinks.add(new CommandGuiLink("AT GL", leftGainBar, leftGainLabel));
            commandGuiLinks.add(new CommandGuiLink("AT GR", rightGainBar, rightGainLabel));
            commandGuiLinks.add(new CommandGuiLink("AT MM", selectStick, selectAlternative));
            commandGuiLinks.add(new CommandGuiLink("AT BM 01", Button1FunctionBox, Button1ParameterText, Button1NumericParameter));
            commandGuiLinks.add(new CommandGuiLink("AT BM 02", Button2FunctionBox, Button2ParameterText, Button2NumericParameter));
            commandGuiLinks.add(new CommandGuiLink("AT BM 03", Button3FunctionBox, Button3ParameterText, Button3NumericParameter));
            commandGuiLinks.add(new CommandGuiLink("AT BM 04", UpFunctionMenu, UpParameterText, UpNumericParameter));
            commandGuiLinks.add(new CommandGuiLink("AT BM 05", DownFunctionMenu, DownParameterText, DownNumericParameter));
            commandGuiLinks.add(new CommandGuiLink("AT BM 06", LeftFunctionMenu, LeftParameterText, LeftNumericParameter));
            commandGuiLinks.add(new CommandGuiLink("AT BM 07", RightFunctionMenu, RightParameterText, RightNumericParameter));
            commandGuiLinks.add(new CommandGuiLink("AT BM 08", SipFunctionMenu, SipParameterText, SipNumericParameter));
            commandGuiLinks.add(new CommandGuiLink("AT BM 09", SpecialSipFunctionMenu, SpecialSipParameterText, SpecialSipNumericParameter));
            commandGuiLinks.add(new CommandGuiLink("AT BM 10", PuffFunctionMenu, PuffParameterText, PuffNumericParameter));
            commandGuiLinks.add(new CommandGuiLink("AT BM 11", SpecialPuffFunctionMenu, SpecialPuffParameterText, SpecialPuffNumericParameter));
        }

        public class Slot
        {
            public String slotName;
            public List<String> settingStrings = new List<String>();
            public Slot()
            {
                slotName = "default";
                settingStrings.Add("AT AX 70"); settingStrings.Add("AT AY 60");
                settingStrings.Add("AT DX 20"); settingStrings.Add("AT DY 20");
                settingStrings.Add("AT TS 500"); settingStrings.Add("AT TP 525");
                settingStrings.Add("AT WS 3"); settingStrings.Add("AT MM 1");
                settingStrings.Add("AT GU 50"); settingStrings.Add("AT GD 50");
                settingStrings.Add("AT GL 50"); settingStrings.Add("AT GR 50");
                settingStrings.Add("AT SM 700"); settingStrings.Add("AT HM 300");

                settingStrings.Add("AT BM 01"); settingStrings.Add("AT NE");
                settingStrings.Add("AT BM 02"); settingStrings.Add("AT KP KEY_ESC ");
                settingStrings.Add("AT BM 03"); settingStrings.Add("AT NC");
                settingStrings.Add("AT BM 04"); settingStrings.Add("AT KP KEY_UP ");
                settingStrings.Add("AT BM 05"); settingStrings.Add("AT KP KEY_DOWN ");
                settingStrings.Add("AT BM 06"); settingStrings.Add("AT KP KEY_LEFT ");
                settingStrings.Add("AT BM 07"); settingStrings.Add("AT KP KEY_RIGHT ");
                settingStrings.Add("AT BM 08"); settingStrings.Add("AT PL");
                settingStrings.Add("AT BM 09"); settingStrings.Add("AT NC");
                settingStrings.Add("AT BM 10"); settingStrings.Add("AT CR");
                settingStrings.Add("AT BM 11"); settingStrings.Add("AT CA");
            }
        };


        public class Command
        {
            public String cmd;
            public int parameterType; // 0=no params, 1=uint paramter, 3=int parameter, 4=string parameter
            public String commandDescription;
            public int comboEntry;
            public int guiType;

            public Command(String cmd, int parameterType, String commandDescription, int comboEntry, int guiType)
            {
                this.cmd = cmd;
                this.parameterType = parameterType;
                this.commandDescription = commandDescription;
                this.comboEntry = comboEntry;
                this.guiType = guiType;
            }
        };

        public class AllCommands
        {
            List<Command> commandList = new List<Command>();

            public void add(Command command)
            {
                commandList.Add(command);
            }
            public String getCommandDescription(int index)
            {
                if ((index>=0)&&(index<commandList.Count))
                    return(commandList[index].commandDescription);
                return (null);
            }
            public int getParameterType(String commandDescription)
            {
                foreach (Command c in commandList)
                    if (c.commandDescription.Equals(commandDescription)) return (c.parameterType);
                return (-1);
            }
            public int getGuiTypeFromDescription(String commandDescription)
            {
                foreach (Command c in commandList)
                    if (c.commandDescription.Equals(commandDescription)) return (c.guiType);
                return (-1);
            }
            public int getGuiTypeFromCommand(String command)
            {
                foreach (Command c in commandList)
                    if (c.cmd.Equals(command)) return (c.guiType);
                return (-1);
            }
            public String getCommand(String commandDescription)
            {
                foreach (Command c in commandList)
                    if (c.commandDescription.Equals(commandDescription)) return (c.cmd);
                return (null);
            }

            public int getComboEntry(String commandDescription)
            {
                foreach (Command c in commandList)
                    if (c.commandDescription.Equals(commandDescription)) return (c.comboEntry);
                return (-1);
            }
            public int getComboEntry(int index)
            {
                if ((index >= 0) && (index < commandList.Count))
                    return (commandList[index].comboEntry);
                return (-1);
            }
            public int getSelectionIndex(int index)
            {
                int selectionIndex = 0;
                if ((index >= 0) && (index < commandList.Count))
                {
                    for (int i = 0; i < index; i++)
                        if (getComboEntry(i) == COMBOENTRY_YES)
                            selectionIndex++;
                }
                return (selectionIndex);
            }
            public int getSelectionIndex(string cmd)
            {
                int selectionIndex = -1;

                for (int i = 0;i < commandList.Count;i++)
                {
                    if (getComboEntry(i) == COMBOENTRY_YES)
                        selectionIndex++;
                    if (cmd.Equals(commandList[i].cmd)) return (selectionIndex);
                }
                return (selectionIndex);
            }
            public int length()
            {
                return commandList.Count;
            }

        };


        public class CommandGuiLink
        {
            public String cmd;

            public ComboBox cb;
            public TextBox tb;
            public NumericUpDown nud;
            public TrackBar tr;
            public Label tl;
            public RadioButton rb1;
            public RadioButton rb2;

            public CommandGuiLink(String cmd, ComboBox cb, TextBox tb, NumericUpDown nud)
            {
                this.cmd = cmd;
                this.cb = cb;
                this.tb = tb;
                this.nud = nud;
            }

            public CommandGuiLink(String cmd, TrackBar tr, Label tl)
            {
                this.cmd = cmd;
                this.tr = tr;
                this.tl = tl;
            }
            public CommandGuiLink(String cmd, RadioButton rb1, RadioButton rb2)
            {
                this.cmd = cmd;
                this.rb1 = rb1;
                this.rb2 = rb2;
            }
        };

        public class CommandGuiLinks
        {
            public List<CommandGuiLink> guiLinks = new List<CommandGuiLink>();

            public void add(CommandGuiLink cgl)
            {
                guiLinks.Add(cgl);
            }
        };


        private String buildCommandString(String cmdDescription, String stringParam, int intParam)
        {
            String str = allCommands.getCommand(cmdDescription);
            switch (allCommands.getParameterType(cmdDescription))
            {
                case PARTYPE_UINT:
                case PARTYPE_INT: str += " " + intParam; break;
                case PARTYPE_STRING: str += " " + stringParam; break;
            }
            return (str);
        }


        public void sendApplyCommands()
        {
            sendCmd("AT AX " + speedXLabel.Text);
            sendCmd("AT AY " + speedYLabel.Text);
            sendCmd("AT DX " + deadzoneXLabel.Text);
            sendCmd("AT DY " + deadzoneYLabel.Text);
            sendCmd("AT TS " + sipThresholdLabel.Text);
            sendCmd("AT TP " + puffThresholdLabel.Text);
            sendCmd("AT SM " + specialThresholdLabel.Text);
            sendCmd("AT HM " + holdThresholdLabel.Text);
            sendCmd("AT GU " + upGainLabel.Text);
            sendCmd("AT GD " + downGainLabel.Text);
            sendCmd("AT GL " + leftGainLabel.Text);
            sendCmd("AT GR " + rightGainLabel.Text);
            if (useAlternativeFunctions)
                sendCmd("AT MM 0");
            else sendCmd("AT MM 1");

            // update the 11 button functions (starting with the 3 physical buttons)
            sendCmd("AT BM 1");
            sendCmd(buildCommandString(Button1FunctionBox.Text, Button1ParameterText.Text, (int)Button1NumericParameter.Value));
            sendCmd("AT BM 2");
            sendCmd(buildCommandString(Button2FunctionBox.Text, Button2ParameterText.Text, (int)Button2NumericParameter.Value));
            sendCmd("AT BM 3");
            sendCmd(buildCommandString(Button3FunctionBox.Text, Button3ParameterText.Text, (int)Button3NumericParameter.Value));
            sendCmd("AT BM 4");
            sendCmd(buildCommandString(UpFunctionMenu.Text, UpParameterText.Text, (int)UpNumericParameter.Value));
            sendCmd("AT BM 5");
            sendCmd(buildCommandString(DownFunctionMenu.Text, DownParameterText.Text, (int)DownNumericParameter.Value));
            sendCmd("AT BM 6");
            sendCmd(buildCommandString(LeftFunctionMenu.Text, LeftParameterText.Text, (int)LeftNumericParameter.Value));
            sendCmd("AT BM 7");
            sendCmd(buildCommandString(RightFunctionMenu.Text, RightParameterText.Text, (int)RightNumericParameter.Value));
            sendCmd("AT BM 8");
            sendCmd(buildCommandString(SipFunctionMenu.Text, SipParameterText.Text, (int)SipNumericParameter.Value));
            sendCmd("AT BM 9");
            sendCmd(buildCommandString(SpecialSipFunctionMenu.Text, SpecialSipParameterText.Text, (int)SpecialSipNumericParameter.Value));
            sendCmd("AT BM 10");
            sendCmd(buildCommandString(PuffFunctionMenu.Text, PuffParameterText.Text, (int)PuffNumericParameter.Value));
            sendCmd("AT BM 11");
            sendCmd(buildCommandString(SpecialPuffFunctionMenu.Text, SpecialPuffParameterText.Text, (int)SpecialPuffNumericParameter.Value));
        }

        public void sendGetIDCommand()
        {
            sendCmd("AT ID");
        }

        public void sendNextCommand()
        {
            sendCmd("AT NE");
        }

        public void sendLoadAllCommand()
        {
            sendCmd("AT LA");
        }

        public void sendEndReportingCommand()
        {
            sendCmd("AT ER");
        }

        public void sendStartReportingCommand()
        {
            sendCmd("AT SR");
        }

        public void sendCalibrationCommand()
        {
           sendCmd("AT CA");
        }

        public void sendLoadSlotCommands(String slotname)
        {
            sendCmd("AT LO " + slotname);
        }

        public void sendSaveSlotCommands(String slotname)
        {
            sendCmd("AT SA " + slotname);
            addToLog("Slot " + slotname + " was stored into FLipmouse.");
        }

        public void sendClearCommand()
        {
            sendCmd("AT DE");
        }
    }
}
