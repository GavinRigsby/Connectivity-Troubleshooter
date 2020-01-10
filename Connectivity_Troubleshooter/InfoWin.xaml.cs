using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
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

namespace Connectivity_Troubleshooter
{
    /// <summary>
    /// Interaction logic for Window2.xaml
    /// </summary>
    
    public partial class InfoWin : Window
    {
        string information;
        public InfoWin()
        {
            InitializeComponent();
        }


        public int fromDigits(List<int> digits, int b) {
            ///Compute the number given by digits in base b.
            int n = 0;
            foreach (int d in digits) {
                n = b * n + d;
                    }
            return n;
        }

        public InfoWin(Json_tool toolkit)
        {
            InitializeComponent();

            //sets heading to Credit union and name
            //if name not given screen prints user
            CU.Text = toolkit.QAmap[1];
            if (!String.IsNullOrEmpty(toolkit.QAmap[2]))
            {
                Intro.Text = toolkit.QAmap[2] + " at";
            }
            //sets up the diagnosis and the errorcode
            this.Diagnosis.Text = toolkit.ErDisplay(toolkit.Ernum);
            this.Raw.Text = toolkit.Ernum.ToString();
            //base 3 to decimal to hexadecimal
            List<int> digits = toolkit.Ernum.Select(x => Convert.ToInt32(x.ToString())).ToList();
            this.Code.Text = fromDigits(digits, 3).ToString("X");

            //runs DisplaySteps and sets steps, description, and contact on page
            toolkit.DisplaySteps();
            this.information = toolkit.steps.ToString();
            this.Contact.Text = toolkit.contacts;

            bool qa = toolkit.QAMap();
            if (qa == true)
            {
                this.QACode.Text = toolkit.QAmap[0];
                this.ProblemMap.Text = toolkit.QAProb;
                this.ProblemName.Text = toolkit.QAName;
                this.QA.Visibility = Visibility.Visible;
                this.QA.Margin = new Thickness(0);
                this.QaBorder.Visibility = Visibility.Visible;
            }


            ColumnDefinition col = new ColumnDefinition();
            col.Width = GridLength.Auto;
            this.descGrid.ColumnDefinitions.Add(col);
            int b = 0;
            for (int i = 0; i < toolkit.desc.Count(); i++)
            {
                
                this.descGrid.Height = Double.NaN;
                descGrid.HorizontalAlignment = HorizontalAlignment.Center;
                RowDefinition newdesc = new RowDefinition();
                RowDefinition newsteps = new RowDefinition();
                newdesc.Height = GridLength.Auto;
                newsteps.Height = GridLength.Auto;
                this.descGrid.RowDefinitions.Add(newdesc);
                this.descGrid.RowDefinitions.Add(newsteps);
                TextBlock descript = new TextBlock();
                descript.Text = toolkit.desc[i];
                descript.VerticalAlignment = VerticalAlignment.Center;
                descript.Margin = new Thickness(0, 5, 0, 0);
                Grid.SetRow(descript, i * 2);
                descGrid.Children.Add(descript);
                string steps = "";
                TextBlock step = new TextBlock();
                while ((toolkit.steps.Count != b + 1) && (toolkit.steps[b][1] < toolkit.steps[b + 1][1]))
                {
                    steps+= toolkit.steps[b];
                    b++;
                }
                steps += toolkit.steps[b];
                b += 1; 
                step.Text = steps;
                step.Margin = new Thickness(0,25,25,5);
                step.TextWrapping = TextWrapping.Wrap;
                step.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetRow(step, (i * 2) + 1);
                
                descGrid.Children.Add(step);
                //<Border CornerRadius="5,5,5,5" BorderThickness="3" Grid.Column="0" Grid.Row="1" Grid.ColumnSpan="3" Padding="1" BorderBrush="DimGray" Margin="20,0,20,0"/>
                Border box = new Border();
                box.CornerRadius = new CornerRadius(5, 5, 5, 5);
                box.BorderThickness = new Thickness(3);
                Grid.SetColumn(box, (i * 2) + 1);
                Grid.SetRow(box, (i * 2) + 1);
                box.Padding = new Thickness(1);
                box.BorderBrush = Brushes.DimGray;
                box.Margin = new Thickness(20, 0, 20, 0);
                box.Width = 550;
                descGrid.Children.Add(box);
                

            }

            HyperLinker(toolkit);
            logger log = new logger(toolkit);
        }

        public void HyperLinker(Json_tool toolkit)
        {
            List<string> Alltext = new List<string>() { this.Contact.Text, this.ProblemMap.Text };
            List<TextBlock> BlockNames = new List<TextBlock>() { this.Contact, this.ProblemMap };
            foreach (var txt in descGrid.Children)
            {
                if (txt.GetType() == typeof(TextBlock)){
                    TextBlock block = (TextBlock)txt;
                    string text = block.Text;
                    if (!string.IsNullOrEmpty(text))
                    {
                        Alltext.Add(text);
                        BlockNames.Add(block);
                    }
                }
            }
            
            
            int blockcounter = -1;
            foreach (string text in Alltext)
            {

                blockcounter += 1;
                if (text.Contains("*"))
                {
                    List<string> linker = new List<string>();
                    List<string> addresses = new List<string>();
                    string info = text;
                    while (true)
                    { 
                        if (!info.Contains("*"))
                        {
                            break;
                        }
                        

                        Dictionary<string, string> Hyperlinks = toolkit.js["Hyperlinks"].ToObject<Dictionary<string, string>>();

                        foreach (KeyValuePair<string, string> link in Hyperlinks)
                        {
                            if (text.Contains("*" + link.Key))
                            {
                                int HyperlinkStart = info.IndexOf("*");
                                string Beforelink = info.Substring(0, HyperlinkStart);
                                linker.Add(Beforelink);
                                int len = link.Key.Length;
                                string LinkID = info.Substring(HyperlinkStart, len + 1);
                                string Afterlink = info.Substring(HyperlinkStart + len + 1);
                                linker.Add(LinkID);
                                addresses.Add(link.Value);
                                info = Afterlink;
                                 
                            }
                        }
                    }
                    linker.Add(info);
                    switch (blockcounter){
                        case 0:
                            Addlink(Contact, linker, addresses, 0);
                            break;
                        case 1:
                            Addlink(ProblemMap, linker, addresses, 0);
                            break;
                        default:
                            Addlink(BlockNames[blockcounter], linker, addresses, 0);
                            break;
                    }
                }
            }
                    
        }
    


        public void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.ToString());
        }

        private void Addlink(TextBlock content, List<string> linker, List<string> address, int start, int itter = 0)
        {
            if(address.Count == itter)
            {
                return;
            }
            if (itter == 0)
            {
                content.Inlines.Clear();
            }
            content.Inlines.Add(linker[start + 0]);
            Hyperlink hyperLink = new Hyperlink()
            {
                NavigateUri = new Uri(address[itter])
            };
            hyperLink.Inlines.Add(linker[start+1].Replace("*",""));
            hyperLink.RequestNavigate += Hyperlink_RequestNavigate;
            content.Inlines.Add(hyperLink);
            Addlink(content, linker, address, start + 2, itter + 1);
            if (address[itter] == address[address.Count -1]){
                content.Inlines.Add(linker[linker.Count -1]);
            }
        }

    }
}
