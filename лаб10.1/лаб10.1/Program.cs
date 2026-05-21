using System;
using System.Threading;

namespace SteamshipLife
{
    // Оголошення делегата для подій пароплава
    public delegate void ShipEventHandler(object sender, ShipEventArgs e);

    /// <summary>
    /// Клас, що задає вхідні й вихідні аргументи подій пароплава
    /// </summary>
    public class ShipEventArgs : EventArgs
    {
        string situation; // Опис того, що сталося (шторм, риф тощо)
        int day;          // День подорожі
        string result;    // ВИХІДНИЙ аргумент: що зробила служба у відповідь

        public string Situation { get { return situation; } }
        public int Day { get { return day; } }
        public string Result
        {
            get { return result; }
            set { result = value; }
        }

        public ShipEventArgs(string situation, int day)
        {
            this.situation = situation;
            this.day = day;
        }
    }

    /// <summary>
    /// Модель пароплава з подіями та службами, що реагують на них
    /// </summary>
    public class Steamship
    {
        // Властивості
        string shipName;       // Назва пароплава
        int travelDays;        // Скільки днів триває плавання
        int fuel = 100;        // Рівень палива (%)
        int integrity = 100;   // Цілісність корпусу (%)

        // Служби / Екіпаж (Спостерігачі)
        Captain captain;
        PortControl portControl;
        Engineers crew;

        // Події пароплава
        public event ShipEventHandler EmergencyEvent;
        string[] resultService; // Масив для збору відповідей від екіпажу/служб

        private Random rnd = new Random();
        double accidentProbability = 0.25; // 25% ймовірність форс-мажору щодня

        /// <summary>
        /// Конструктор пароплава. Створює екіпаж та підключає їх до подій
        /// </summary>
        public Steamship(string name, int days)
        {
            shipName = name;
            travelDays = days;

            // Створення служб
            captain = new Captain(this);
            portControl = new PortControl(this);
            crew = new Engineers(this);

            // Включення спостереження (підписка на подію)
            captain.On();
            portControl.On();
            crew.On();
        }

        /// <summary>
        /// Метод викликає подію та збирає звіти від усіх служб
        /// </summary>
        protected virtual void OnEmergency(ShipEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"🚢 [Подія] На пароплаві \"{shipName}\" халепа: {e.Situation}! (День {e.Day}-й)");
            Console.ResetColor();

            if (EmergencyEvent != null)
            {
                // Отримуємо список усіх, хто підписаний на подію
                Delegate[] eventHandlers = EmergencyEvent.GetInvocationList();
                resultService = new string[eventHandlers.Length];
                int k = 0;

                // По черзі опитуємо кожну службу
                foreach (ShipEventHandler evhandler in eventHandlers)
                {
                    evhandler(this, e);
                    resultService[k++] = e.Result; // Записуємо, що зробила служба
                }
            }
        }

        /// <summary>
        /// Моделювання плавання пароплава
        /// </summary>
        public void StartVoyage()
        {
            Console.WriteLine($"⚓ Пароплав \"{shipName}\" знімається з якоря і виходить у море на {travelDays} днів!\n");
            bool safeArrival = true;

            for (int day = 1; day <= travelDays; day++)
            {
                Console.WriteLine($" День {day}-й плавання ");

                // Щодня витрачається паливо на звичайний хід
                fuel -= 15;

                // Перевірка на випадкову критичну подію
                if (rnd.NextDouble() < accidentProbability)
                {
                    // Випадково обираємо тип аварії
                    string[] accidents = { "Потужний шторм", "Зіткнення з підводним рифом", "Загоряння у вугільному трюмі" };
                    string currentAccident = accidents[rnd.Next(accidents.Length)];

                    // Наносимо шкоду пароплаву перед реакцією служб
                    if (currentAccident.Contains("шторм")) { integrity -= 25; fuel -= 10; }
                    if (currentAccident.Contains("риф")) integrity -= 40;
                    if (currentAccident.Contains("трюм")) { integrity -= 15; fuel -= 20; }

                    // Створюємо аргументи події та запускаємо її
                    ShipEventArgs e = new ShipEventArgs(currentAccident, day);
                    OnEmergency(e);

                    // Виводимо звіти служб, які відреагували на подію
                    if (resultService != null)
                    {
                        foreach (string report in resultService)
                        {
                            if (report != null) Console.WriteLine("   -> " + report);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("🌊 Море спокійне. Пароплав іде за заданим курсом.");
                }

                // Виводимо поточний стан корабля в кінці дня
                Console.WriteLine($"📊 Стан корабля: Паливо: {fuel}%, Міцність корпусу: {integrity}%");
                // Перевірка на повну катастрофу
                if (integrity <= 0 || fuel <= 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n🚨 КАТАСТРОФА! Пароплав \"{shipName}\" не може продовжувати рух (Корпус: {integrity}%, Паливо: {fuel}%).");
                    Console.ResetColor();
                    safeArrival = false;
                    break;
                }

                Thread.Sleep(1000); // Невелика пауза для красивого виведення в консоль
                Console.WriteLine();
            }

            if (safeArrival)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n🎉 Ура! Пароплав \"{shipName}\" успішно зайшов у порт призначення!");
                Console.ResetColor();
            }
        }
    }
    public abstract class ShipReceiver
    {
        protected Steamship ship;
        protected Random rnd = new Random();

        public ShipReceiver(Steamship ship)
        {
            this.ship = ship;
        }

        // Підписка на подію
        public void On()
        {
            ship.EmergencyEvent += new ShipEventHandler(HandleEmergency);
        }

        // Відписка від події
        public void Off()
        {
            ship.EmergencyEvent -= new ShipEventHandler(HandleEmergency);
        }

        // Абстрактний метод обробки, який реалізує кожен підрозділ
        public abstract void HandleEmergency(object sender, ShipEventArgs e);
    }
    // 1. Капітан міняє курс або заспокоює пасажирів
    public class Captain : ShipReceiver
    {
        public Captain(Steamship ship) : base(ship) { }
        public override void HandleEmergency(object sender, ShipEventArgs e)
        {
            if (e.Situation.Contains("шторм"))
                e.Result = "👨‍✈️ Капітан: «Наказав змінити курс у затишну бухту, шторм перечекаємо!»";
            else if (e.Situation.Contains("риф"))
                e.Result = "👨‍✈️ Капітан: «Оголошено тривогу! Не панікувати, ситуація під контролем!»";
            else
                e.Result = "👨‍✈️ Капітан: «Евакуювати пасажирів з палуби ближче до рятувальних шлюпок!»";
        }
    }

    // 2. Інженери та матроси латають діри чи гасять вогонь
    public class Engineers : ShipReceiver
    {
        public Engineers(Steamship ship) : base(ship) { }
        public override void HandleEmergency(object sender, ShipEventArgs e)
        {
            if (e.Situation.Contains("трюм"))
                e.Result = "🔧 Трюмна команда: «Усі вогнегасники задіяні, пожежу локалізовано!»";
            else if (e.Situation.Contains("риф"))
                e.Result = "🔧 Інженери: «Пробоїна серйозна! Встановили тимчасовий пластир, качаємо воду насосами!»";
            else
                e.Result = "🔧 Матроси: «Укріпили вантаж на палубі, перевіряємо задраювання ілюмінаторів.»";
        }
    }

    // 3. Портовий контроль висилає допомогу або фіксує SOS
    public class PortControl : ShipReceiver
    {
        public PortControl(Steamship ship) : base(ship) { }
        public override void HandleEmergency(object sender, ShipEventArgs e)
        {
            // Шанс успішно відправити допомогу з берега
            if (rnd.Next(0, 10) > 4)
                e.Result = "📡 Берегова охорона: «Координати прийнято! Назустріч вам виїхав рятувальний буксир.»";
            else
                e.Result = "📡 Порт: «Вас чуємо погано через перешкоди. Слідкуємо за вашим GPS-маяком.»";
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            // Підтримка українського тексту в консолі
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Створюємо пароплав "Титанік-2", який має плисти 7 днів
            Steamship myShip = new Steamship("Титанік-2", 7);

            // Запускаємо симуляцію подорожі
            myShip.StartVoyage();

            Console.WriteLine("\nНатисніть Enter для виходу з програми...");
            Console.ReadLine();
        }
    }
}