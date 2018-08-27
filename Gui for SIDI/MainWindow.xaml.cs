using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Gui_for_SIDI
{
    public partial class MainWindow : Window
    {
        Account[] Accs;
        Game[] Games;
        Settings settings;
        int numInGroup;
        public MainWindow()
        {
            InitializeComponent();


            DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(Settings));
            using (FileStream fs = new FileStream("settings.json", FileMode.OpenOrCreate))
            {
                try
                {
                    settings = (Settings)jsonFormatter.ReadObject(fs);
                }
                catch (SerializationException) { }
            }
            if (settings == null) SetSettings();
            else
            {
                tb_timeout.Text = settings.PlayTimeout.ToString();
                tb_groupac.Text = settings.GroupSize.ToString();
            }

            numInGroup = int.Parse(tb_groupac.Text);
            jsonFormatter = new DataContractJsonSerializer(typeof(Account[]));
            using (FileStream fs = new FileStream("accounts.json", FileMode.OpenOrCreate))
            {
                try
                {
                    Accs = (Account[])jsonFormatter.ReadObject(fs);
                }
                catch (SerializationException){}
            }
            jsonFormatter = new DataContractJsonSerializer(typeof(Game[]));
            using (FileStream fs = new FileStream("games.json", FileMode.OpenOrCreate))
            {
                try
                {
                    Games = (Game[])jsonFormatter.ReadObject(fs);
                }
                catch (SerializationException) { }
            }
            StandartGames();
            ShowGames();
        }

        private void SetSettings()
        {
            if (settings == null) settings = new Settings(int.Parse(tb_timeout.Text), int.Parse(tb_groupac.Text));
            else
            {
                settings.PlayTimeout = int.Parse(tb_timeout.Text);
                settings.GroupSize = int.Parse(tb_groupac.Text);
            }
            File.WriteAllText("settings.json", string.Empty);
            DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(Settings));
            using (FileStream fs = new FileStream("settings.json", FileMode.OpenOrCreate))
            {
                jsonFormatter.WriteObject(fs, settings);
            }
        }

        private void ShowGames()
        {
            cb_games_list.Items.Clear();
            if (Games != null)
            {
                for (int i = 0; i < Games.Length; ++i)
                {
                    cb_games_list.Items.Add(Games[i].Name);
                }
                cb_games_list.SelectedItem = 1;
            }
        }

        private void StandartGames()
        {
            if (Games != null) return;
            DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(Game[]));
            using (FileStream fs = new FileStream("games.json", FileMode.OpenOrCreate))
            {
                Games = new Game[] {
                    new Game("Call to Arms", 302670, 10),
                    new Game("Immune", 348670, 100),
                    new Game("Killing Floor 2", 232090, 910000),
                    new Game("PAYDAY 2", 218620, 1),
                    new Game("Rust", 252490, 10),
                    new Game("Savage Resurrection", 366440, 121),
                    new Game("Team Fortress 2", 440, 0),
                    new Game("Unturned", 304930, 10000)
                };
                jsonFormatter.WriteObject(fs, Games);
            }
            Show_games_manage();
        }

        private void But_start_Click(object sender, RoutedEventArgs e)
        {
            string gamename = (string)cb_games_list.SelectedItem;
            if (gamename == null)
            {
                MessageBox.Show("Game is not selected","Warning");
                return;
            }

            int timeout = int.Parse(tb_timeout.Text);
            if(timeout < 1)
            {
                MessageBox.Show("Timeout should be more than 1 second", "Warning");
                tb_timeout.Text = "1";
                return;
            }
            else if (timeout >3600)
            {
                MessageBox.Show("Timeout should be less than 3600 seconds (1 hour)", "Warning");
                tb_timeout.Text = "3600";
                return;
            }

            SetSettings();

            int id = 0,definition = 0;
            for(int i = 0; i < Games.Length; ++i)
            {
                if(gamename == Games[i].Name)
                {
                    id = Games[i].ID;
                    definition = Games[i].Definition;
                    break;
                }
            }
            if ((bool)rb_all.IsChecked) {
                Start_Games(Accs, id, definition);
            }
            else if ((bool)rb_choose.IsChecked) {
                List<Account> ac = new List<Account>();
                foreach (Object obj in sp_chooseaccs.Children)
                {
                    if (obj.GetType() == typeof(CheckBox))
                    {
                        CheckBox a = (CheckBox)obj;
                        if (a.IsChecked == true)
                        {
                            for (int i = 0; i < Accs.Length; ++i)
                            {
                                if (a.Content.ToString() == Accs[i].Login)
                                {
                                    ac.Add(Accs[i]);
                                    break;
                                }
                            }
                        }
                    }
                }
                Start_Games(ac.ToArray(),id,definition);
            }
            else if ((bool)rb_static.IsChecked)
            {
                List<Account> accsgroup = new List<Account>();
                int whichrb = 0;
                foreach (Object obj in sp_staticaccs.Children)
                {
                    if (obj.GetType() == typeof(RadioButton))
                    {
                        RadioButton rb = (RadioButton)obj;
                        if (rb.IsChecked == true) break;
                        ++whichrb;
                    }
                }

                int acnum = whichrb * numInGroup;
                try
                {
                    for (int i = acnum; i < acnum + numInGroup; ++i)
                    {
                        accsgroup.Add(Accs[i]);
                    }
                }
                catch (IndexOutOfRangeException) { }
                Start_Games(accsgroup.ToArray(), id, definition);
            }
        }
        private void Start_Games(Account[] array, int id, int definition)
        {
            if (array == null) return;
            ProcessStartInfo startInfo = new ProcessStartInfo(System.AppDomain.CurrentDomain.BaseDirectory + "SteamItemDropIdler\\SteamItemDropIdler.exe");
            startInfo.WindowStyle = ProcessWindowStyle.Minimized;
            for (int i = 0; i < array.Length; ++i)
            {
                startInfo.Arguments = "" + array[i].Login + " " + array[i].Password + " " + id + " " + definition;
                Process.Start(startInfo);
                if (i != array.Length - 1)
                {
                    int timeout = int.Parse(tb_timeout.Text);
                    System.Threading.Thread.Sleep(timeout * 1000);
                }
                
            }
        }

        private void But_addac_Click(object sender, RoutedEventArgs e)
        {
            if (tb_login.Text == "" || tb_pass.Password == "")
            {
                MessageBox.Show("Fields should't be empty", "Warning");
            }
            else
            {
                Account acc = new Account(tb_login.Text, tb_pass.Password);
                bool ifExist = false;
                if (Accs != null)
                {
                    for (int i = 0; i < Accs.Length; ++i)
                    {
                        if (acc.Login == Accs[i].Login)
                        {
                            MessageBox.Show("This login already exists", "Warning");
                            ifExist = true;
                            break;
                        }
                    }
                }
                if (!ifExist)
                {
                    DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(Account[]));
                    using (FileStream fs = new FileStream("accounts.json", FileMode.OpenOrCreate))
                    {
                        if (Accs == null) Accs = new Account[] { acc };
                        else
                        {
                            Array.Resize(ref Accs, Accs.Length + 1);
                            Accs[Accs.Length - 1] = acc;
                        }
                        jsonFormatter.WriteObject(fs, Accs);
                    }
                    Show_accs_manage();
                }
            }
        }

        private void But_delac_Click(object sender, RoutedEventArgs e)
        {
            int count = 0;
            foreach (Object obj in st_deleteacs.Children)
            {
                if (obj.GetType() == typeof(CheckBox))
                {
                    CheckBox a = (CheckBox)obj;
                    if(a.IsChecked == true)
                    {
                        for(int i = 0; i < Accs.Length; ++i)
                        {
                            if (Accs[i] == null) continue;
                            if (a.Content.ToString() == Accs[i].Login) {
                                Accs[i] = null;
                                count++;
                            }
                        }
                    }
                }
            }
            Account[] newaccs = new Account[Accs.Length - count];
            count = 0;
            for(int i = 0; i < Accs.Length; ++i)
            {
                if(Accs[i] != null) newaccs[count++] = Accs[i];
            }
            Accs = newaccs;
            File.WriteAllText("accounts.json", string.Empty);
            DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(Account[]));
            using (FileStream fs = new FileStream("accounts.json", FileMode.OpenOrCreate))
            {
                jsonFormatter.WriteObject(fs, Accs);
            }
            Show_accs_manage();
        }

        private void Expander_DelAcs(object sender, RoutedEventArgs e)
        {
            Show_accs_manage();
        }

        private void Show_accs_manage()
        {
            st_deleteacs.Children.Clear();
            sp_chooseaccs.Children.Clear();
            if (Accs != null)
            {
                for (int i = 0; i < Accs.Length; ++i)
                {
                    st_deleteacs.Children.Add(new CheckBox { Content = Accs[i].Login });
                    sp_chooseaccs.Children.Add(new CheckBox { Content = Accs[i].Login });
                }
            }
        }

        private void But_addgame_Click(object sender, RoutedEventArgs e)
        {
            if (tb_gamename.Text == "" || tb_gameid.Text == "" || tb_definition.Text == "")
            {
                MessageBox.Show("Fields should't be empty","Warning");
            }
            else
            {
                Game game = new Game(tb_gamename.Text, int.Parse(tb_gameid.Text), int.Parse(tb_definition.Text));
                bool ifExist = false;
                if (Games != null)
                {
                    for (int i = 0; i < Games.Length; ++i)
                    {
                        if (game.ID == Games[i].ID)
                        {
                            MessageBox.Show("This id already exists", "Warning");
                            ifExist = true;
                            break;
                        }
                    }
                }
                if (!ifExist)
                {
                    DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(Game[]));
                    using (FileStream fs = new FileStream("games.json", FileMode.OpenOrCreate))
                    {
                        if (Games == null) Games = new Game[] { game };
                        else
                        {
                            Array.Resize(ref Games, Games.Length + 1);
                            Games[Games.Length - 1] = game;
                        }
                        jsonFormatter.WriteObject(fs, Games);
                    }
                    Show_games_manage();
                    ShowGames();
                }
            }
        }

        private void But_delgame_Click(object sender, RoutedEventArgs e)
        {
            int count = 0;
            foreach (Object obj in st_deletegame.Children)
            {
                if (obj.GetType() == typeof(CheckBox))
                {
                    CheckBox a = (CheckBox)obj;
                    if (a.IsChecked == true)
                    {
                        for (int i = 0; i < Games.Length; ++i)
                        {
                            if (Games[i] == null) continue;
                            if (a.Content.ToString() == Games[i].Name)
                            {
                                Games[i] = null;
                                count++;
                            }
                        }
                    }
                }
            }
            Game[] newgames = new Game[Games.Length - count];
            count = 0;
            for (int i = 0; i < Games.Length; ++i)
            {
                if (Games[i] != null) newgames[count++] = Games[i];
            }
            Games = newgames;
            File.WriteAllText("games.json", string.Empty);
            DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(Game[]));
            using (FileStream fs = new FileStream("games.json", FileMode.OpenOrCreate))
            {
                jsonFormatter.WriteObject(fs, Games);
            }
            Show_games_manage();
            ShowGames();
        }

        private void Expander_DelGames(object sender, RoutedEventArgs e)
        {
            Show_games_manage();
        }

        private void Show_games_manage()
        {
            st_deletegame.Children.Clear();
            if (Games != null)
            {
                for (int i = 0; i < Games.Length; ++i)
                {
                    st_deletegame.Children.Add(new CheckBox { Content = Games[i].Name });
                }
            }
        }

        private void Show_static_groups()
        {
            numInGroup = int.Parse(tb_groupac.Text);
            if(numInGroup < 2)
            {
                MessageBox.Show("Value must be more than 1","Warning");
                tb_groupac.Text = "2";
                numInGroup = 2;
            }
            int amount = Accs.Length / numInGroup;
            sp_staticaccs.Children.Clear();
            for (int i = 0; i < amount+1; ++i)
            {
                if(i == amount)
                {
                    int lastgroup = Accs.Length % numInGroup;
                    if (lastgroup == 1) sp_staticaccs.Children.Add(new RadioButton { Name = "rb_group_" + i, GroupName = "gn_group_num", Content = (i * numInGroup + 1) });
                    else if (lastgroup != 0) sp_staticaccs.Children.Add(new RadioButton { Name = "rb_group_" + i, GroupName = "gn_group_num", Content = (i * numInGroup + 1) + " - " +(i * numInGroup + lastgroup)});
                }
                else
                {
                    sp_staticaccs.Children.Add(new RadioButton {Name="rb_group_"+i, GroupName = "gn_group_num", Content = (i * numInGroup + 1) + " - " + (numInGroup * (i + 1)) });
                }
            }
        }

        private void tb_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, "[^0-9]+");
        }

        private void RadioButton_All_Checked(object sender, RoutedEventArgs e)
        {
            if(gb_chooseaccs != null) gb_chooseaccs.Visibility = Visibility.Collapsed;
            if (gb_staticaccs != null) gb_staticaccs.Visibility = Visibility.Collapsed;
        }

        private void RadioButton_Choose_Checked(object sender, RoutedEventArgs e)
        {
            if (gb_staticaccs != null) gb_staticaccs.Visibility = Visibility.Collapsed;
            Show_accs_manage();
            gb_chooseaccs.Visibility = Visibility.Visible;
        }

        private void RadioButton_Static_Checked(object sender, RoutedEventArgs e)
        {
            if (gb_chooseaccs != null) gb_chooseaccs.Visibility = Visibility.Collapsed;
            Show_static_groups();
            gb_staticaccs.Visibility = Visibility.Visible;
        }

        private void But_updatestatic_Click(object sender, RoutedEventArgs e)
        {
            Show_static_groups();
            SetSettings();
        }
    }

    [DataContract]
    public class Account
    {
        [DataMember]
        public string Login { get; set; }
        [DataMember]
        public string Password { get; set; }

        public Account(string login, string password)
        {
            Login = login;
            Password = password;
        }
    }

    [DataContract]
    public class Game
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public int ID { get; set; }
        [DataMember]
        public int Definition { get; set; }

        public Game(string name, int id, int definition)
        {
            Name = name;
            ID = id;
            Definition = definition;
        }
    }

    [DataContract]
    public class Settings
    {
        [DataMember]
        public int PlayTimeout { get; set; }
        [DataMember]
        public int GroupSize { get; set; }

        public Settings(int playtimeout, int groupsize)
        {
            PlayTimeout = playtimeout;
            GroupSize = groupsize;
        }
    }
}
