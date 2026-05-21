using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SteamshipLifeAdvanced
{
    // 1. Пріоритет подій (чим менше число, тим вищий пріоритет у PriorityQueue)
    public enum EventPriority
    {
        Critical = 1, // Катастрофічні події (пробоїна)
        Medium = 2,   // Помірні (шторм)
        Low = 3       // Низькі (закінчується паливо, дрібний ремонт)
    }

    public delegate Task ShipAsyncEventHandler(object sender, ShipEventArgs e);

    /// <summary>
    /// Аргументи події, що включають пріоритет та асинхронну відповідь
    /// </summary>
    public class ShipEventArgs : EventArgs
    {
        public string Situation { get; }
        public int Day { get; }
        public EventPriority Priority { get; }

        // Зберігаємо відповіді від різних служб в потокобезпечний список
        public List<string> Results { get; } = new List<string>();

        public ShipEventArgs(string situation, int day, EventPriority priority)
        {
            Situation = situation;
            Day = day;
            Priority = priority;
        }
    }

    /// <summary>
    /// Клас для збору статистики за період плавання
    /// </summary>
    public class VoyageStatistics
    {
        public int TotalAccidents { get; set; } = 0;
        public int CriticalEventsCount { get; set; } = 0;
        public int FuelSpent { get; set; } = 0;
        public int TotalRepairs { get; set; } = 0;

        public void DisplayReport(int totalDays)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"📊 ЗВІТ ПРО СТАТИСТИКУ ПЛАВАННЯ ЗА {totalDays} ДНІВ:");
            Console.WriteLine($"• Всього зафіксовано пригод: {TotalAccidents}");
            Console.WriteLine($"• З них критичних аварій: {CriticalEventsCount}");
            Console.WriteLine($"• Витрачено палива за рейс: {FuelSpent}%");
            Console.WriteLine($"• Проведено ремонтних операцій: {TotalRepairs}");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Модель пароплава з чергою пріоритетів та асинхронним обробником
    /// </summary>
    public class Steamship
    {
        private string shipName;
        private int travelDays;
        private int fuel = 100;
        private int integrity = 100;

        // Екіпаж
        private Captain captain;
        private Engineers crew;
        private PortControl portControl;

        // Статистика
        public VoyageStatistics Stats { get; } = new VoyageStatistics();

        // Черга подій: (Аргументи події, Пріоритет)
        private PriorityQueue<ShipEventArgs, int> eventQueue = new PriorityQueue<ShipEventArgs, int>();

        // Асинхронна подія
        public event ShipAsyncEventHandler EmergencyAsyncEvent;
        public Steamship(string name, int days)
        {
            shipName = name;
            travelDays = days;

            captain = new Captain(this);
            crew = new Engineers(this);
            portControl = new PortControl(this);

            captain.On();
            crew.On();
            portControl.On();
        }

        /// <summary>
        /// Додавання події в чергу з урахуванням її пріоритету
        /// </summary>
        public void EnqueueEmergency(string situation, int day, EventPriority priority)
        {
            ShipEventArgs args = new ShipEventArgs(situation, day, priority);
            eventQueue.Enqueue(args, (int)priority); // Пріоритет визначається числом (1 - найвищий)

            // Фіксуємо для статистики
            Stats.TotalAccidents++;
            if (priority == EventPriority.Critical) Stats.CriticalEventsCount++;
        }

        /// <summary>
        /// Асинхронна обробка черги подій
        /// </summary>
        private async Task ProcessEventQueueAsync()
        {
            while (eventQueue.Count > 0)
            {
                // Витягуємо найбільш пріоритетну подію
                ShipEventArgs currentEvent = eventQueue.Dequeue();

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n[Черга] Обробка події: \"{currentEvent.Situation}\" (Пріоритет: {currentEvent.Priority}, День {currentEvent.Day})");
                Console.ResetColor();

                if (EmergencyAsyncEvent != null)
                {
                    Delegate[] handlers = EmergencyAsyncEvent.GetInvocationList();
                    List<Task> tasks = new List<Task>();
                    // Запускаємо обробники всіх служб паралельно (асинхронно)
                    foreach (ShipAsyncEventHandler handler in handlers)
                    {
                        tasks.Add(handler(this, currentEvent));
                    }
                    // Чекаємо, поки всі служби завершать роботу над цією подією
                    await Task.WhenAll(tasks);
                    // Виводимо звіти дій служб
                    foreach (string report in currentEvent.Results)
                    {
                        Console.WriteLine("   -> " + report);
                    }
                }
            }
        }

        /// <summary>
        /// Головний асинхронний цикл життя пароплава
        /// </summary>
        public async Task StartVoyageAsync()
        {
            Console.WriteLine($"⚓ Пароплав \"{shipName}\" вирушає в асинхронне плавання на {travelDays} днів!\n");
            Random rnd = new Random();

            for (int day = 1; day <= travelDays; day++)
            {
                Console.WriteLine($"\n☀️День {day}-й розпочався");

                // Витрата палива на хід
                fuel -= 15;
                Stats.FuelSpent += 15;

                // 1. Моделювання випадкових ситуацій та наповнення черги подій
                if (rnd.NextDouble() < 0.4) // 40% шанс на пригоду
                {
                    // Симулюємо появу кількох подій одночасно в один день
                    EnqueueEmergency("Брак вугілля в топці", day, EventPriority.Low);
                    EnqueueEmergency("Пробоїна від підводного рифу", day, EventPriority.Critical);
                    EnqueueEmergency("Штормове попередження", day, EventPriority.Medium);
                }

                // 2. Асинхронно обробляємо чергу подій за цей день відповідно до пріоритетів
                if (eventQueue.Count > 0)
                {
                    await ProcessEventQueueAsync();
                }
                else
                {
                    Console.WriteLine("🌊 День пройшов спокійно, черга пригод порожня.");
                }

                // Коригування стану корабля після дій служб (імітація наслідків)
                integrity = Math.Max(0, integrity - (rnd.Next(5, 15)));

                Console.WriteLine($"📊 Підсумок дня {day}: Паливо: {fuel}%, Корпус: {integrity}%");

                if (integrity <= 0 || fuel <= 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n🚨 КАТАСТРОФА! Пароплав \"{shipName}\" затонув на {day}-й день.");
                    Console.ResetColor();
                    break;
                }

                // Асинхронна затримка між днями (не блокує потік)
                await Task.Delay(1000);
            }

            // 3. Виведення статистики за весь період після завершення плавання
            Stats.DisplayReport(travelDays);
        }
    }
    public abstract class ShipReceiver
    {
        protected Steamship ship;
        protected Random rnd = new Random();

        // Залишаємо один єдиний правильний конструктор
        public ShipReceiver(Steamship ship)
        {
            this.ship = ship ?? throw new ArgumentNullException(nameof(ship));
        }

        public void On() => ship.EmergencyAsyncEvent += HandleEmergencyAsync;
        public void Off() => ship.EmergencyAsyncEvent -= HandleEmergencyAsync;

        public abstract Task HandleEmergencyAsync(object sender, ShipEventArgs e);
    }
    public class Captain : ShipReceiver
    {
        public Captain(Steamship ship) : base(ship) { }

        public override async Task HandleEmergencyAsync(object sender, ShipEventArgs e)
        {
            // Імітуємо час на прийняття рішення капітаном (залежно від критичності)
            int decisionTime = e.Priority == EventPriority.Critical ? 200 : 500;
            await Task.Delay(decisionTime);

            lock (e.Results)
            {
                e.Results.Add($"👨‍✈️ Капітан: Прийняв рішення щодо \"{e.Situation}\". Пріоритет дії: {e.Priority}.");
            }
        }
    }

    public class Engineers : ShipReceiver
    {
        public Engineers(Steamship ship) : base(ship) { }

        public override async Task HandleEmergencyAsync(object sender, ShipEventArgs e)
        {
            // Інженери довше латають пробоїни
            if (e.Priority == EventPriority.Critical)
            {
                await Task.Delay(800); // Час на ремонт
                ship.Stats.TotalRepairs++;
                lock (e.Results) e.Results.Add("🔧 Інженери: КРИТИЧНО! Пробоїну залатано, воду відкачано!");
            }
            else
            {
                await Task.Delay(300);
                lock (e.Results) e.Results.Add("🔧 Інженери: Технічна проблема вирішена в штатному режимі.");
            }
        }
    }

    public class PortControl : ShipReceiver
    {
        public PortControl(Steamship ship) : base(ship) { }

        public override async Task HandleEmergencyAsync(object sender, ShipEventArgs e)
        {
            await Task.Delay(400); // Час на радіозв'язок
            lock (e.Results)
            {
                string response = e.Priority == EventPriority.Critical
                    ? "📡 Берег: SOS прийнято! Висилаємо рятувальні катери!"
                    : "📡 Берег: Повідомлення взято до відома. Оновіть координати при зміні курсу.";
                e.Results.Add(response);
            }
        }
    }
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            // Створюємо пароплав на 5 днів плавання
            Steamship myShip = new Steamship("Дніпро", 5);
            // Запускаємо асинхронну подорож
            await myShip.StartVoyageAsync();
            Console.WriteLine("Натисніть Enter для завершення...");
            Console.ReadLine();
        }
    }
}
