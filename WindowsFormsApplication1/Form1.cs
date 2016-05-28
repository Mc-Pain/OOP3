using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        volatile bool working = false;

        private void Modelling()
        { //здесь будет моделирование
            int i = 0;
            while (working)
            {
                if (i > 1000)
                    i = 0;
                Invoke(new OutputDelegate(Output), new object[] { String.Format("Работа... {0}", i++) });
            }
            Invoke(new OutputDelegate(Output), new object[] { String.Format("Останов: {0}", i) });
        }

        private void Output(string msg)
        {
            wklabel.Text = msg;
            wklabel.Update();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Thread thr = new Thread(new ThreadStart(Modelling));
            thr.IsBackground = true;
            thr.Start();
            if (working)
            {
                button1.Text = "Запуск";
            }
            else
            {
                button1.Text = "Остановить";
            }
            working = !working;
        }
    }

    public delegate void OutputDelegate(string msg);

    public class _Exception : Exception
    {
        public _Exception(string message)
        {

        }
    }

    public class CPlane
    {
        private CPassenger[] passengers;
        private CFlight flight;
        private bool landed;
        private CAirport landed_in;
        private int num_engaged; //занято мест

        private void CPlane(CAirport airport)
        {
            landed = true;
            landed_in = airport;
            num_engaged = 0;
            flight = null;
            passengers = new CPassenger[100];
        }

        private void Land(CAirport airport)
        {
            if (!landed)
            {
                landed = true;
                landed_in = airport;
            }
            else
            {
                _Exception exc = new _Exception("Только Чаке может сажать самолёт два раза подряд");
                throw (exc);
            }
        }

        private void Takeoff()
        {
            if (landed)
            {

                landed_in = null;
                landed = false;
            }
            else
            {
                _Exception exc = new _Exception("Самолет уже летит, а ты его запустить хочешь");
                throw (exc);
            }
        }

        private void take_passenger(CPassenger passenger, CTicket ticket)
        {
            if (landed)
            {
                if (passengers[ticket.GetSeat()] == null)
                {
                    passengers[ticket.GetSeat()] = passenger;
                    num_engaged++;
                }
                else
                {
                    _Exception exc = new _Exception("Вы сели на моё место!");
                    throw (exc);
                }
            }
            else
            {
                _Exception exc = new _Exception("Супермена чтоли собрался сажать?");
                throw (exc);
            }
        }

        private void kick_passenger(int seat)
        {
            if (landed)
            {
                if (passengers[seat] != null)
                {
                    passengers[seat] = null;
                    num_engaged--;
                }
            }
            else
            {
                _Exception exc = new _Exception("THIS IS SPARTA!!!1");
                throw (exc);
            }
        }

        private void setflight(CFlight new_flight)
        {
            if (flight != null)
            {
                _Exception exc = new _Exception("Менять рейс во время полёта - не труЪ");
                throw (exc);
            }
            else
            {
                flight = new_flight;
            }
        }
    }

    public class CFlight
    {
        private string name; //идентификатор рейса
        private CAirport start; //начало рейса
        private CAirport end; //конец рейса
        private DateTime takeoff; //время взлета
        private DateTime landing; //время посадки
        private CPlane plane; //самолет
        private CTicket[] tickets; //билеты на рейс
    }

    public class CPassenger
    {
        private DateTime arrival; //прибытие в аэропорт
        private CTicket ticket; //билет
    }

    public class CAirport
    {
        private string name;
        private Queue<CPlane> planequeue;
        private List<CPassenger> passengerlist;
    }

    public class CTicket
    {
        private CPassenger passenger;
        private CFlight flight;
        private int seat;

        public int GetSeat()
        {
            return seat;
        }
    }
}
