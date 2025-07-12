using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections;
using System.Security.Cryptography;

namespace MAMEInteropWindow
{
    public class MAMEInterop : IDisposable
    {
        [DllImport("MAME32.dll", EntryPoint = "init_mame", CallingConvention = CallingConvention.StdCall)]
        static extern int init_mame32(int clientid, string name, [MarshalAs(UnmanagedType.FunctionPtr)] MAME_START start, [MarshalAs(UnmanagedType.FunctionPtr)] MAME_STOP stop, [MarshalAs(UnmanagedType.FunctionPtr)] MAME_COPYDATA copydata, [MarshalAs(UnmanagedType.FunctionPtr)] MAME_OUTPUT output, bool useNetworkOutput);

        [DllImport("MAME32.dll", EntryPoint = "close_mame", CallingConvention = CallingConvention.StdCall)]
        static extern int close_mame32();

        [DllImport("MAME32.dll", EntryPoint = "message_mame", CallingConvention = CallingConvention.StdCall)]
        static extern int message_mame32(int id, int value);

        [DllImport("MAME64.dll", EntryPoint = "init_mame", CallingConvention = CallingConvention.StdCall)]
        static extern int init_mame64(int clientid, string name, [MarshalAs(UnmanagedType.FunctionPtr)] MAME_START start, [MarshalAs(UnmanagedType.FunctionPtr)] MAME_STOP stop, [MarshalAs(UnmanagedType.FunctionPtr)] MAME_COPYDATA copydata, [MarshalAs(UnmanagedType.FunctionPtr)] MAME_OUTPUT output, bool useNetworkOutput);

        [DllImport("MAME64.dll", EntryPoint = "close_mame", CallingConvention = CallingConvention.StdCall)]
        static extern int close_mame64();

        [DllImport("MAME64.dll", EntryPoint = "message_mame", CallingConvention = CallingConvention.StdCall)]
        static extern int message_mame64(int id, int value);



        // COPYDATASTRUCT definition
        [StructLayout(LayoutKind.Sequential)]
        private struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        // Import SendMessage from user32.dll
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);

        // WM_COPYDATA constant
        private const uint WM_COPYDATA = 0x004A;

        private delegate int MAME_START(IntPtr hwnd);
        private delegate int MAME_STOP();
        private delegate int MAME_COPYDATA(int id, IntPtr name);
        private delegate int MAME_OUTPUT(IntPtr name, int value);
        // Public-facing “message_mame” IDs
        public enum MAMEMessageType : int
        {
            Pause = 0,
            SaveState = 1,
            LoadState = 2
        }


        [MarshalAs(UnmanagedType.FunctionPtr)]
        private MAME_START startPtr = null;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        private MAME_STOP stopPtr = null;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        private MAME_COPYDATA copydataPtr = null;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        private MAME_OUTPUT outputPtr = null;

        private Control m_control = null;
        private IntPtr m_hWndOutputWindow = IntPtr.Zero;

        public event EventHandler<MAMEEventArgs> MAMEStart;
        public event EventHandler<EventArgs> MAMEStop;
        public event EventHandler<MAMECopydataEventArgs> MAMECopydata;
        public event EventHandler<MAMEOutputEventArgs> MAMEOutput;
        public event EventHandler MAMEPause;
        public event EventHandler<SaveStateEventArgs> MAMESaveState;
        public event EventHandler<LoadStateEventArgs> MAMELoadState;
        private bool m_isRunning = false;
        private int lastPid;
        private bool m_is64Bit = false;
        private bool m_disposed = false;

        public MAMEInterop(Control control)
        {
            m_control = control;
            m_is64Bit = Is64Bit();
        }

        public void Initialize(int clientId, string name, bool useNetworkOutput)
        {
            m_is64Bit = Is64Bit();

            startPtr = new MAME_START(mame_start);
            stopPtr = new MAME_STOP(mame_stop);
            copydataPtr = new MAME_COPYDATA(mame_copydata);
            outputPtr = new MAME_OUTPUT(mame_output);

            if (m_is64Bit)
                init_mame64(clientId, name, startPtr, stopPtr, copydataPtr, outputPtr, useNetworkOutput);
            else
                init_mame32(clientId, name, startPtr, stopPtr, copydataPtr, outputPtr, useNetworkOutput);

            m_isRunning = true;
        }

        public void Shutdown()
        {
            if (m_is64Bit)
                close_mame64();
            else
                close_mame32();

            m_isRunning = false;
        }

        private int mame_start(IntPtr hWnd)
        {
            m_hWndOutputWindow = hWnd;

            return 1;
        }


        private int mame_stop()
        {
            m_hWndOutputWindow = IntPtr.Zero;

            if (MAMEStop != null)
                m_control.BeginInvoke(MAMEStop, this, EventArgs.Empty);

            return 1;
        }

        // Called for id==0 (ROM start) and id>0 (lamp names)
        private int mame_copydata(int id, IntPtr namePtr)
        {
            string s = Marshal.PtrToStringAnsi(namePtr) ?? "";
            if (id == 0)
            {
                string rom = s;
                int pid = lastPid;        // the PID we saved in mame_start
                m_control.BeginInvoke(MAMEStart, this, new MAMEEventArgs(pid, rom));
            }
            else
            {
                // Lamp name registration
                m_control.BeginInvoke(MAMECopydata, this, new MAMECopydataEventArgs(id, s));
            }
            return 1;
        }

        // Called for lamp‐state updates
        private int mame_output(IntPtr namePtr, int state)
        {
            string s = Marshal.PtrToStringAnsi(namePtr) ?? "";
            m_control.BeginInvoke(MAMEOutput, this, new MAMEOutputEventArgs(s, state));
            return 1;
        }

        public int MessageMAME(int id, int value)
        {
            int retVal = 0;

            if (m_is64Bit)
                retVal = message_mame64(id, value);
            else
                retVal = message_mame32(id, value);

            return retVal;
        }

        public int PauseMAME(int pauseValue)
        {
            return m_is64Bit
                ? message_mame64((int)MAMEMessageType.Pause, pauseValue)
                : message_mame32((int)MAMEMessageType.Pause, pauseValue);
        }

        /// <summary>Save state into the given slot.</summary>
        public int SaveState(int slot)
        {
            return m_is64Bit
                ? message_mame64((int)MAMEMessageType.SaveState, slot)
                : message_mame32((int)MAMEMessageType.SaveState, slot);
        }

        /// <summary>Load state from the given slot.</summary>
        public int LoadState(int slot)
        {
            return m_is64Bit
                ? message_mame64((int)MAMEMessageType.LoadState, slot)
                : message_mame32((int)MAMEMessageType.LoadState, slot);
        }

        // === Enhanced MAMEInterop Request Method ===
        public void RequestLampName(int lampId)
        {
            if (m_hWndOutputWindow != IntPtr.Zero)
            {
                COPYDATASTRUCT cds = new COPYDATASTRUCT
                {
                    dwData = new IntPtr(1), // Custom identifier for validation requests
                    cbData = sizeof(int),
                    lpData = Marshal.AllocHGlobal(sizeof(int))
                };

                Marshal.WriteInt32(cds.lpData, lampId);

                SendMessage(m_hWndOutputWindow, WM_COPYDATA, m_control.Handle, ref cds);
                Marshal.FreeHGlobal(cds.lpData);
            }
        }

        public bool IsRunning
        {
            get { return m_isRunning; }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // remove this from gc finalizer list
        }

        private void Dispose(bool disposing)
        {
            if (!this.m_disposed) // dispose once only
            {
                if (disposing) // called from Dispose
                {
                    // Dispose managed resources.
                }

                // Clean up unmanaged resources here.
            }

            m_disposed = true;
        }

        #endregion

        private bool Is64Bit()
        {
            return Marshal.SizeOf(typeof(IntPtr)) == 8;
        }
    }

    public class MAMEEventArgs : EventArgs
    {
        public int PID { get; }
        public string ROMName { get; }

        public MAMEEventArgs(string romName)
        {
            ROMName = romName;
        }
        public MAMEEventArgs(int pid, string romName)
        {
            PID = pid;
            ROMName = romName;
        }
    }
    public class MAMEStopEventArgs : EventArgs
    {
        public int PID { get; }
        public MAMEStopEventArgs(int pid) { PID = pid; }
    }

    public class MAMEOutputEventArgs : EventArgs
    {
        public string Name;
        public int State;

        public MAMEOutputEventArgs(string name, int state)
        {
            Name = name;
            State = state;
        }
    }
    public class SaveStateEventArgs : EventArgs
    {
        public int Slot { get; }
        public SaveStateEventArgs(int slot) => Slot = slot;
    }

    public class LoadStateEventArgs : EventArgs
    {
        public int Slot { get; }
        public LoadStateEventArgs(int slot) => Slot = slot;
    }

    public enum MAMEMessageType : int
    {
        Pause = 0,
        SaveState = 1
    };

    public class MAMECopydataEventArgs : EventArgs
    {
        public int Id { get; }
        public string Name { get; }

        public MAMECopydataEventArgs(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}