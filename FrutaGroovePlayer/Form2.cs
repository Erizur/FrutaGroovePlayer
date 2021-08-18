using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace FrutaGroovePlayer
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox1.Checked == true)
            {
                Properties.Settings.Default.updateStartup = true;
            }
            else
            {
                Properties.Settings.Default.updateStartup = false;
            }   
        }

        private void button2_Click(object sender, EventArgs e)
        {
            AboutBox1 ab1 = new AboutBox1();
            ab1.Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string downloadUrl = "";
            Version newVer = null;
            string xmlUrl = "";
            XmlTextReader reader = null;
            try
            {
                reader = new XmlTextReader(xmlUrl);
                reader.MoveToContent();
                string elementName = "";
                if((reader.NodeType == XmlNodeType.Element) && (reader.Name == "frutagroove"))
                {
                    while (reader.Read())
                    {
                        if(reader.NodeType == XmlNodeType.Element)
                        {
                            elementName = reader.Name;
                        }
                        else
                        {
                            if((reader.NodeType == XmlNodeType.Text) && (reader.HasValue))
                            {
                                switch (elementName)
                                {
                                    case "version":
                                        newVer = new Version(reader.Value);
                                        break;
                                    case "url":
                                        downloadUrl = reader.Value;
                                        break;
                                }
                            }
                        }
                    }   
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                if(reader != null)
                {
                    reader.Close();
                }
                Version applicationVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                if (applicationVersion.CompareTo(newVer) < 0)
                {
                    DialogResult result = MessageBox.Show("FrutaGroove Player Version " + newVer.Major + "." + newVer.Minor + "." + newVer.Build + " is now available to download. Update Now?", "New Version Available", MessageBoxButtons.YesNo);
                    if(result == DialogResult.Yes)
                    {

                    }
                    else if(result == DialogResult.No)
                    {

                    }
                }
            }
        }
    }
}
