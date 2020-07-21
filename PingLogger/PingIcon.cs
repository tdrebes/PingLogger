using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PingLogger
{
    class PingIcon
    {
        public static NotifyIcon nIcon = MainWindow.nIcon;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr FindWindow(string ClassName, string WindowText);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);

        public static Icon CreateTextIcon(string str)
        {
            int fontsize;
            if (str.Length >= 3)
            {
                fontsize = 10;
            }
            else
            {
                fontsize = 16;
            }
            Font fontToUse = new Font("Microsoft Sans Serif", fontsize, FontStyle.Regular, GraphicsUnit.Pixel);
            Brush brushToUse = new SolidBrush(Color.White);
            Bitmap bitmapText = new Bitmap(16, 16);
            Graphics g = System.Drawing.Graphics.FromImage(bitmapText);

            g.Clear(Color.Transparent);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
            g.DrawString(str, fontToUse, brushToUse, -4, -2);
            IntPtr hIcon = bitmapText.GetHicon();
            Icon icon = System.Drawing.Icon.FromHandle(hIcon);
            brushToUse.Dispose();
            bitmapText.Dispose();
            g.Dispose();

            return icon;

        }


        public static void updatePing()
        {
            while (true)
            {
                using (Ping p = new Ping())
                {
                    try
                    {
                        var ping = PingModule.GetPing(PingModule.ip);
                        if (nIcon.Icon != null) nIcon.Icon.Dispose();
                        Icon icon = CreateTextIcon(ping.ToString());
                        nIcon.Icon = icon;
                        DestroyIcon(icon.Handle);

                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }
                }

                Thread.Sleep(1000);

            }
        }

    }
}
