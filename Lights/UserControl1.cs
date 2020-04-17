using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Lights
{
    public partial class UserControl1 : UserControl
    {
        ulong State;
        ulong Pressed;
        ulong Xor;
        ulong Highlight;
        ulong[] Matrix;
        const int GWidth = 8;
        const int GHeight = 8;

        public UserControl1()
        {
            Matrix = genPuzzle(out State, out Xor);
            InitializeComponent();
        }

        public void NewPuzzle()
        {
            Pressed = 0;
            Highlight = 0;
            Xor = 0;
            Matrix = genPuzzle(out State, out Xor);
            Refresh();
        }

        static ulong[] genPuzzle(out ulong On, out ulong Xor)
        {
            On = 0;
            Xor = 0;
            Random r = new Random();
            ulong[] Matrix = new ulong[64];
            for (int i = 0; i < Matrix.Length; i++)
            {
                Matrix[i] = 1UL << i;
            }

            for (int i = 0; i < 100000; i++)
            {
                int r0 = r.Next(64);
                int r1 = r.Next(64);
                if (r0 == r1)
                    continue;

                Matrix[r0] ^= Matrix[r1];
            }

            ulong[] inv2 = inverse(Matrix);

            ulong a = 0;
            ulong x = 0;
            do
            {
                x = r.Next64();
                Xor = x;
                On = Xor;
                a = mulv(inv2, Xor);
            } while (!InRange(popcnt(a), 4, 20));

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    Console.Write(a.TestBit(j, i) ? "X" : ".");
                }
                Console.WriteLine();
            }

            return Matrix;
        }

        static bool InRange(int value, int low, int high)
        {
            return value >= low && value <= high;
        }

        static int popcnt(int x)
        {
            x = x - ((x >> 1) & 0x55555555);
            x = (x & 0x33333333) + ((x >> 2) & 0x33333333);
            x = (x & 0x0f0f0f0f) + ((x >> 4) & 0x0f0f0f0f);
            return (x * 0x01010101) >> 24;
        }

        static int popcnt(ulong x)
        {
            int r = popcnt((int)x) + popcnt((int)(x >> 32));
            return r;
        }


        static ulong mulv(ulong[] mat, ulong vec)
        {
            ulong r = 0;
            for (int i = 0; i < mat.Length; i++)
            {
                if ((vec & 1) != 0)
                    r ^= mat[i];
                vec >>= 1;
            }
            return r;
        }

        static ulong[] inverse(ulong[] mat)
        {
            mat = mat.Clone() as ulong[];
            ulong[] inv = new ulong[64];
            for (int n = 0; n < 64; n++)
                inv[n] = 1UL << n;

            for (int n = 0; n < 64; n++)
            {
                if ((mat[n] & (1UL << n)) == 0)
                {
                    for (int i = n + 1; i < 64; i++)
                    {
                        if ((mat[i] & (1UL << n)) != 0)
                        {
                            swap(ref mat[i], ref mat[n]);
                            swap(ref inv[i], ref inv[n]);
                            break;
                        }
                    }
                }
                for (int i = 0; i < 64; i++)
                {
                    if (i == n) continue;
                    if ((mat[i] & (1UL << n)) != 0)
                    {
                        mat[i] ^= mat[n];
                        inv[i] ^= inv[n];
                    }
                }
            }
            return inv;
        }

        static void swap<T>(ref T a, ref T b)
        {
            T t = a;
            a = b;
            b = t;
        }

        private void UserControl1_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            double colWidth = (double)Bounds.Width / GWidth;
            double rowHeight = (double)Bounds.Height / GHeight;

            for (int y = 0; y < GHeight; y++)
            {
                int yc = (int)(y * rowHeight);
                int yc2 = (int)((y + 1) * rowHeight);
                for (int x = 0; x < GWidth; x++)
                {
                    int xc = (int)(x * colWidth);
                    int xc2 = (int)((x + 1) * colWidth);

                    if (Highlight.TestBit(x, y))
                        g.FillRectangle(Brushes.LightBlue, new Rectangle(xc, yc, xc2 - xc, yc2 - yc));
                }
            }

            g.DrawRectangle(Pens.Black, new Rectangle(0, 0, Bounds.Width - 1, Bounds.Height - 1));

            for (int x = 1; x < GWidth; x++)
            {
                int xc = (int)(x * colWidth);
                g.DrawLine(Pens.Black, new Point(xc, 0), new Point(xc, Bounds.Height));
            }

            for (int y = 1; y < GHeight; y++)
            {
                int yc = (int)(y * rowHeight);
                g.DrawLine(Pens.Black, new Point(0, yc), new Point(Bounds.Width, yc));
            }

            Pen p = new Pen(Color.Black, 3);

            for (int y = 0; y < GHeight; y++)
            {
                int yc = (int)(y * rowHeight);
                int yc2 = (int)((y + 1) * rowHeight);
                for (int x = 0; x < GWidth; x++)
                {
                    int xc = (int)(x * colWidth);
                    int xc2 = (int)((x + 1) * colWidth);

                    g.DrawString((1 + x + GWidth * y).ToString(), this.Font, Brushes.Black, new Point(xc, yc));

                    if (State.TestBit(x, y))
                    {
                        g.DrawLine(p, new Point(xc + 15, yc + 15), new Point(xc2 - 15, yc2 - 15));
                        g.DrawLine(p, new Point(xc + 15, yc2 - 15), new Point(xc2 - 15, yc + 15));
                    }
                    else
                    {
                        g.DrawEllipse(p, new Rectangle(xc + 15, yc + 15, xc2 - xc - 30, yc2 - yc - 30));
                    }

                    if (Pressed.TestBit(x, y))
                        g.FillRectangle(Brushes.Black, xc2 - 5, yc + 1, 5, 5);
                }
            }
        }

        private Point PointToCell(Point p)
        {
            double colWidth = (double)Bounds.Width / GWidth;
            double rowHeight = (double)Bounds.Height / GHeight;
            for (int y = 0; y < GHeight; y++)
            {
                int yc = (int)(y * rowHeight);
                int yc2 = (int)((y + 1) * rowHeight);
                for (int x = 0; x < GWidth; x++)
                {
                    int xc = (int)(x * colWidth);
                    int xc2 = (int)((x + 1) * colWidth);

                    if (new Rectangle(xc, yc, xc2 - xc, yc2 - yc).Contains(p))
                        return new Point(x, y);
                }
            }

            return Point.Empty;
        }

        private void UserControl1_MouseMove(object sender, MouseEventArgs e)
        {
            var cell = PointToCell(e.Location);

            ulong newhl = Matrix[cell.X + cell.Y * 8];
            if (Highlight != newhl)
            {
                Highlight = newhl;
                Refresh();
            }
        }

        private void UserControl1_MouseLeave(object sender, EventArgs e)
        {
            Highlight = 0;
            Refresh();
        }

        private void UserControl1_MouseClick(object sender, MouseEventArgs e)
        {
            var p = PointToCell(e.Location);

            Pressed ^= 1UL << (p.X + p.Y * 8);
            State = mulv(Matrix, Pressed) ^ Xor;
            Refresh();

            if (State == 0)
            {
                MessageBox.Show("Puzzle solved!");
                NewPuzzle();
            }
        }
    }

    static class Ext
    {
        public static bool TestBit(this ulong v, int x, int y)
        {
            return (v & (1UL << (x + 8 * y))) != 0;
        }

        public static ulong Next64(this Random r)
        {
            byte[] t = new byte[8];
            r.NextBytes(t);
            return BitConverter.ToUInt64(t, 0);
        }
    }
}
