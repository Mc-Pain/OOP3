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

        int num = 6;
        volatile bool working = false;
        static DateTime globaltime = new DateTime(2016, 05, 01);
        System.Timers.Timer timer = new System.Timers.Timer(100);
        CAirport[] airports;
        CPlane[] planes;
        Random rand = new Random();
        int flight_no = 0;

        private void Init()
        {
            Thread thr_model = new Thread(new ThreadStart(Modelling));
            thr_model.IsBackground = true;
            Thread thr_time = new Thread(new ThreadStart(Timing));
            thr_time.IsBackground = true;
            airports = new CAirport[num];
            planes = new CPlane[num];
            for (int i = 0; i < num; i++)
            { //создаём аэропорты и самолёты
                airports[i] = new CAirport(i.ToString());
                planes[i] = new CPlane(airports[i]);

                Invoke(new OutputDelegate(Output), new object[] { String.Format("Работа... {0}", globaltime.ToString("f")) });
            }
            thr_model.Start();
            thr_time.Start();
            button1.Text = "Остановить";
        }

        private void UnInit()
        {
            timer.Elapsed -= OnTimedEvent;
            button1.Text = "Запуск";
        }

        private void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            globaltime = globaltime.AddMinutes(1);
            Invoke(new ModellingDelegate(Modelling));
        }

        private void Timing()
        {
            timer.Elapsed += OnTimedEvent;
            timer.Start();
        }

        private void Modelling()
        { //здесь будет моделирование
            Invoke(new state_OutputDelegate(state_out), new object[] { false, "" });
            for (int i = 0; i < num; i++)
            { //самолёты
                string label_text = String.Format("Самолет {0}: Рейс:", i);

                if (planes[i].GetFlight() == null)
                { //если полет не задан
                    label_text += String.Format(" не задан\n");

                    int PortNext = rand.Next(num);
                    while (planes[i].GetPort() == airports[PortNext])
                    { //проверка на случай, если место прибытия и место отправления - один и тот же аэропорт
                        PortNext = rand.Next(num);
                    }
                    DateTime takeoff = globaltime.AddHours(3);
                    DateTime landing = globaltime.AddMinutes(360 + rand.Next(360));

                    //создаем новый полет и назначаем его
                    CFlight flight = new CFlight(flight_no.ToString(), planes[i].GetPort(), airports[PortNext], takeoff, landing);
                    flight_no++;

                    Random rand_ = new Random();
                    for (int index = 0; index < 100; index++)
                    { //генерация билетов и пассажиров
                        if (rand_.Next(100) < rand_.Next(60, 95))
                        { //вероятность покупки билета - 60-95%
                            CPassenger pass = new CPassenger(flight, index);
                            planes[i].take_passenger(pass);
                        }
                    }

                    planes[i].setflight(flight);
                }
                else
                { //если полёт уже задан
                    if (globaltime.AddMinutes(-10) < planes[i].GetFlight().GetLanding())
                    {
                        planes[i].GetFlight().GetEnd().busy = true;
                    }

                    label_text += String.Format("\nИмя: {0}, Место отправления: {1}, Место прибытия: {2}, Время отправления: {3}, Время прибытия: {4}\nСтатус: ",
                        planes[i].GetFlight().GetName(), planes[i].GetFlight().GetStart().GetName(), planes[i].GetFlight().GetEnd().GetName(),
                        planes[i].GetFlight().GetTakeOff(), planes[i].GetFlight().GetLanding()); //ойжуть

                    if (planes[i].IsLanded())
                    {
                        label_text += String.Format("В аэропорту {0}", planes[i].GetPort().GetName());
                    }
                    else
                    {
                        label_text += "В воздухе";
                    }

                    if (planes[i].GetFlight().TakingOff(globaltime))
                    {
                        planes[i].Takeoff();
                        planes[i].GetFlight().GetEnd().SetLandingNext(planes[i]);
                        label_text += " (Взлёт)";
                    }

                    if (planes[i].GetFlight().Landing(globaltime))
                    {
                        label_text += " (Посадка)";
                        planes[i].Land(planes[i].GetFlight().GetEnd());
                        for (int j = 0; j < 100; j++)
                        { //генерация билетов и пассажиров
                            planes[i].kick_passenger(j);
                        }
                    }
                    label_text += String.Format(", Число пассажиров: {0}", planes[i].GetEngaged());

                    label_text += "\n";
                }
                Invoke(new state_OutputDelegate(state_out), new object[] { true, label_text });
            }
            Invoke(new OutputDelegate(Output), new object[] { String.Format("Текущее время: {0}", globaltime.ToString("f")) });
        }

        private void state_out(bool flag, string msg)
        {
            if (flag)
            {
                statelabel.Text += msg + "\n";
                statelabel.Update();
            }
            else
            {
                statelabel.Text = msg;
                statelabel.Update();
            }
        }

        private void Output(string msg)
        {
            wklabel.Text = msg;
            wklabel.Update();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            working = !working;
            if (working)
            {
                Init();
            }
            else
            {
                UnInit();
            }
        }
    }

    public delegate void OutputDelegate(string msg);

    public delegate void state_OutputDelegate(bool flag, string msg);

    public delegate void ModellingDelegate();

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

        public CFlight GetFlight()
        {
            return flight;
        }

        public CAirport GetPort()
        {
            return landed_in;
        }

        public CPlane(CAirport airport)
        { //создание самолёта
            landed = true;
            landed_in = airport;
            num_engaged = 0;
            flight = null;
            passengers = new CPassenger[100];
        }

        public void Land(CAirport airport)
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

        public void Takeoff()
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

        public void take_passenger(CPassenger passenger)
        { //приём пассажира на борт
            if (landed)
            { //самолёт не в воздухе
                if (passengers[passenger.GetSeat()] == null)
                { //место свободно
                    passengers[passenger.GetSeat()] = passenger;
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

        public void kick_passenger(int seat)
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

        public void setflight(CFlight new_flight)
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

        public int GetEngaged()
        {
            return num_engaged;
        }

        public bool IsLanded()
        {
            return landed;
        }
    }

    public class CFlight
    {
        private string name; //идентификатор рейса
        private CAirport start; //начало рейса
        private CAirport end; //конец рейса
        private DateTime takeoff; //время взлета
        private DateTime landing; //время посадки

        public bool TakingOff(DateTime globaltime)
        {
            if (DateTime.Compare(takeoff, globaltime) == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Landing(DateTime globaltime)
        {
            if (DateTime.Compare(landing, globaltime) == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public CFlight(string id, CAirport new_start, CAirport new_end, DateTime new_takeoff, DateTime new_landing)
        { //создание рейса
            name = id;
            start = new_start;
            end = new_end;
            takeoff = new_takeoff;
            landing = new_landing;
        }

        public void Delay(int min)
        { //задержать рейс
            takeoff = takeoff.AddMinutes(min);
            landing = landing.AddMinutes(min);
        }

        public DateTime GetTakeOff()
        {
            return takeoff;
        }

        public DateTime GetLanding()
        {
            return landing;
        }

        public string GetName()
        {
            return name;
        }

        public CAirport GetEnd()
        {
            return end;
        }

        public CAirport GetStart()
        {
            return start;
        }
    }

    public class CPassenger
    {
        private CFlight flight;
        private int seat;

        public CPassenger(CFlight new_flight, int new_seat)
        { //создание пассажира
            flight = new_flight;
            seat = new_seat;
        }

        public int GetSeat()
        {
            return seat;
        }
    }

    public class CAirport
    {
        private CPlane landing;
        private string name;
        public bool busy = false;

        public CAirport(string new_name)
        {
            name = new_name;
        }

        public CPlane LandingNext()
        {
            return landing;
        }

        public void SetLandingNext(CPlane plane)
        {
            landing = plane;
        }

        public string GetName()
        {
            return name;
        }

        public bool Busy()
        {
            return busy;
        }
    }
}
