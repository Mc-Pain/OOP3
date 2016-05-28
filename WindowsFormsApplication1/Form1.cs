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
        static DateTime globaltime = new DateTime(2016, 05, 01);

        private void Init()
        {
            int num = 6;
            CAirport[] airports = new CAirport[num];
            CPlane[] planes = new CPlane[num];
            for (int i = 0; i < num; i++)
            { //создаём аэропорты и самолёты
                airports[i] = new CAirport(i.ToString());
                planes[i] = new CPlane(airports[i]);

                Invoke(new OutputDelegate(Output), new object[] { String.Format("Работа... {0}", globaltime.ToString("f")) });
            }
        }

        private static void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        { 
            globaltime = globaltime.AddMinutes(10);
        }

        private void Timing()
        {
            System.Timers.Timer timer = new System.Timers.Timer(500);
            timer.Elapsed += OnTimedEvent;
            timer.Start();
        }

        private void Modelling()
        { //здесь будет моделирование
            while (working)
            {
                Invoke(new OutputDelegate(Output), new object[] { String.Format("Работа... {0}", globaltime.ToString("f")) });
            }
            Invoke(new OutputDelegate(Output), new object[] { String.Format("Останов {0}", globaltime.ToString("f")) });
        }

        private void Output(string msg)
        {
            wklabel.Text = msg;
            wklabel.Update();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Thread thr_model = new Thread(new ThreadStart(Modelling));
            thr_model.IsBackground = true;
            thr_model.Start();
            Thread thr_time = new Thread(new ThreadStart(Timing));
            thr_time.IsBackground = true;
            thr_time.Start();
            working = !working;
            if (working)
            {
                Init();
                button1.Text = "Остановить";
            }
            else
            {
                button1.Text = "Запуск";
            }
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

        public CPlane(CAirport airport)
        { //создание самолёта
            landed = true;
            landed_in = airport;
            num_engaged = 0;
            flight = null;
            passengers = new CPassenger[100];
        }

        private void Land(CAirport airport)
        { //приземление
            if (!landed)
            { //самолёт в воздухе
                landed = true;
                landed_in = airport;
                for (int i = 0; i < 100; i++)
                {
                    this.kick_passenger(i); //высаживаем i-го пассажира
                }
                airport.busy = false;
                flight = null; //задание выполнено
            }
            else
            { //самолёт не в воздухе
                _Exception exc = new _Exception("Только Чаке может сажать самолёт два раза подряд");
                throw (exc);
            }
        }

        private void Takeoff()
        { //взлёт
            if (landed)
            { //самолёт не в воздухе
                landed_in = null;
                landed = false;
            }
            else
            { //самолёт в воздухе
                _Exception exc = new _Exception("Самолет уже летит, а ты его запустить хочешь");
                throw (exc);
            }
        }

        private void take_passenger(CPassenger passenger, CTicket ticket)
        { //приём пассажира на борт
            if (landed)
            { //самолёт не в воздухе
                if (passengers[ticket.GetSeat()] == null)
                { //место свободно
                    passengers[ticket.GetSeat()] = passenger;
                    num_engaged++;
                }
                else
                { //место занято
                    _Exception exc = new _Exception("Вы сели на моё место!");
                    throw (exc);
                }
            }
            else
            { //самолёт в воздухе
                _Exception exc = new _Exception("Супермена чтоли собрался сажать?");
                throw (exc);
            }
        }

        private void kick_passenger(int seat)
        { //высадка пассажиров
            if (landed)
            { //самолёт не в воздухе
                if (passengers[seat] != null)
                {
                    passengers[seat] = null;
                    num_engaged--;
                }
            }
            else
            { //самолёт в воздухе
                _Exception exc = new _Exception("THIS IS SPARTA!!!1");
                throw (exc);
            }
        }

        private void setflight(CFlight new_flight)
        { //установка нового рейса
            if (flight != null)
            { //рейс уже установлен
                _Exception exc = new _Exception("Менять рейс во время полёта - не труЪ");
                throw (exc);
            }
            else
            { //рейс не установлен
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

        private CFlight(string id, CAirport new_start, CAirport new_end, DateTime new_takeoff, DateTime new_landing)
        { //создание рейса
            name = id;
            start = new_start;
            end = new_end;
            takeoff = new_takeoff;
            landing = new_landing;
            tickets = new CTicket[100];

            Random rand = new Random();
            for (int i = 0; i < 100; i++)
            { //генерация билетов и пассажиров
                if (rand.Next(100) < 90)
                { //вероятность покупки билета - 90%
                    int time = 10 + rand.Next(170);
                    CPassenger pass = new CPassenger(takeoff);
                    tickets[i] = new CTicket(pass, this, i);
                }
            }
        }

        private void Delay(int min)
        { //задержать рейс
            takeoff = takeoff.AddMinutes(min);
            landing = landing.AddMinutes(min);
        }
    }

    public class CPassenger
    {
        private DateTime arrival; //прибытие в аэропорт
        private CTicket ticket; //билет

        public CPassenger(DateTime new_arrival)
        { //создание пассажира
            new_arrival = arrival;
        }

        public void SetTicket(CTicket new_ticket)
        {
            if (ticket == null)
            {
                new_ticket = ticket;
            }
            else
            {
                _Exception exc = new _Exception("Зачем чуваку два билета?");
                throw (exc);
            }
        }
    }

    public class CAirport
    {
        private string name;
        public bool busy = false;

        public CAirport(string new_name)
        {
            name = new_name;
        }
    }

    public class CTicket
    {
        private CPassenger passenger;
        private CFlight flight;
        private int seat;

        public CTicket(CPassenger new_passenger, CFlight new_flight, int number)
        {
            passenger = new_passenger;
            flight = new_flight;
            seat = number;
        }

        public int GetSeat()
        {
            return seat;
        }
    }
}
