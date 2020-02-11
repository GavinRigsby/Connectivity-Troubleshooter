using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Drawing;
using Pen = System.Drawing.Pen;

[System.Windows.Localizability(System.Windows.LocalizationCategory.None, Readability = System.Windows.Readability.Unreadable)]
public abstract class Shape : System.Windows.FrameworkElement { }

namespace Connectivity_Troubleshooter
{
    public partial class MainWindow : Window
    {

        Json_tool toolkit = new Json_tool();
        public MainWindow()
        {
            InitializeComponent();
            Object Unions = toolkit.PopulateCU(this);
            if (Unions is ComboBox)
            {
                this.ComboCU = (ComboBox)Unions;
            }
            else
            {
                this.TxtCU = (TextBox)Unions;
            }
        }

        public ComboBox ComboCU { get; private set; }
        public TextBox TxtCU { get; private set; }

        private void Start_Test(object sender, RoutedEventArgs e)
        {
            //Some validation to check if Credit Union is filled in & radiobuttons are clicked
            //items turn red when user clickes button to continue
            //all red items must be filled before continuing to the scan

            if (ComboCU != null)
            {
                if (ComboCU.Text == " ")
                {
                    ComboCU.BorderBrush = System.Windows.Media.Brushes.Red;
                    ComboCU.Background = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    ComboCU.BorderBrush = System.Windows.Media.Brushes.White;
                    ComboCU.Background = System.Windows.Media.Brushes.White;
                }
            }
            if (TxtCU != null)
            {
                if (String.IsNullOrEmpty(TxtCU.Text))
                {
                    TxtCU.BorderBrush = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    TxtCU.BorderBrush = System.Windows.Media.Brushes.Gray;
                }
            }
            if ((one.IsChecked ?? false) || (mult.IsChecked ?? false))
            {
                one.BorderBrush = System.Windows.Media.Brushes.Gray;
                mult.BorderBrush = System.Windows.Media.Brushes.Gray;
            }
            else
            {
                one.BorderBrush = System.Windows.Media.Brushes.Red;
                mult.BorderBrush = System.Windows.Media.Brushes.Red;
            }
            if ((Intermittent.IsChecked ?? false) || (Constant.IsChecked ?? false))
            {
                Intermittent.BorderBrush = System.Windows.Media.Brushes.Gray;
                Constant.BorderBrush = System.Windows.Media.Brushes.Gray;
            }
            else
            {
                Intermittent.BorderBrush = System.Windows.Media.Brushes.Red;
                Constant.BorderBrush = System.Windows.Media.Brushes.Red;
            }
            if (((TxtCU != null && !String.IsNullOrEmpty(TxtCU.Text)) || (ComboCU != null && !String.IsNullOrEmpty(ComboCU.Text))) && ((one.IsChecked ?? false) || (mult.IsChecked ?? false)) && ((Intermittent.IsChecked ?? false) || (Constant.IsChecked ?? false)))
            { 
                //takes all user inputs and makes the QAMAP with the QA number the users Credit Union, Name, etc..
                string CreditUnion = CU.Text;
                string Name = name.Text;
                string Email = email.Text;
                string Issues = OtherIssues.Text;
                int bit1 = 0, bit2 = 0, bit3 = 0, bit4 = 0;
                if (one.IsChecked ?? false)
                {
                    bit1 = 1;
                }
                if (Intermittent.IsChecked ?? false)
                {
                    bit2 = 1;
                }
                if (Affected.IsChecked ?? false)
                {
                    bit3 = 1;
                }
                if (OtherAffected.IsChecked ?? false)
                {
                    bit4 = 1;
                }
                string QAnum = (bit1.ToString() + bit2.ToString() + bit3.ToString() + bit4.ToString());
                string[] QAMAP = { QAnum, CreditUnion, Name, Email, Issues};
                //creates new Json_tool to handle json queries
                
                toolkit.QAmap = QAMAP;
                toolkit.start();




                //runs the ping scan to find the error code 
                
                Window1 window1 = new Window1(toolkit);
                window1.Show();
                
                




                //test codes without running full scan
                //comment out above section and uncomment below section
                /*
                toolkit.Ernum = "1101010";
                InfoWin Info = new InfoWin(toolkit);
                Info.Show();
                */

                this.Close();
            }
        }
    }
}
