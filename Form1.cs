using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace XVICouchTrends
{
    public partial class Form1 : Form
    {
        public string PatientsCSVPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\patient_folders.csv";
        public List<string> PatientsPaths;
        public DataSet dataSet;

        public Form1()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;

            if (!File.Exists(this.PatientsCSVPath))
            {
                MessageBox.Show("File patient_folders.csv does not exist.", "Error");
                this.Close();
            }

            ReadPatientPaths();

            int directoryExist = 0;
            foreach (var patient in this.PatientsPaths)
            {
                if (!Directory.Exists(patient))
                {
                    ++directoryExist;
                }
            }
            if (directoryExist > 0)
            {
                MessageBox.Show("Not all patient folder paths are valid.\nFix patient_folders.csv and try again.", "Error");
                this.Close();
            }

            CreateDataset();
        }


        private void button1_Click(object sender, EventArgs e)
        {
            ResetDataset();

            foreach (var patient in this.PatientsPaths)
            {
                List<List<string>> imgList = CollectDataForPatient(patient);

                Dictionary<string, string> scaninixvi = new Dictionary<string, string>() { };
                Dictionary<string, string> scanini = new Dictionary<string, string>() { };
                
                foreach (var img in imgList)
                {
                    foreach (var file in img)
                    {
                        if (file.Contains(".INI.XVI"))
                        {
                            scaninixvi = ScanINIXVI(file);
                            if (scaninixvi.Count > 0)
                            {
                                break;
                            }
                        }
                    }
                    foreach (var file in img)
                    {
                        if (file.Contains(".INI"))
                        {
                            scanini = ScanINI(file);
                            if (scanini.Count > 0)
                            {
                                break;
                            }
                        }
                    }
                    if (scaninixvi.Count > 0 & scanini.Count > 0)
                    {
                        AddToDataset(scaninixvi, scanini);
                    }
                }
            }
            SetcomboBoxPatient();
            //ShowTable();
        }

        private void ResetDataset()
        {
            this.dataSet.Clear();
            this.dataSet.Tables["data"].DefaultView.RowFilter = string.Empty;
        }

        private void SetcomboBoxPatient()
        {
            List<string> PatientIDs = new List<string>() { };
            for (int i=0; i<this.dataSet.Tables["data"].Rows.Count; i++)
            {
                PatientIDs.Add(this.dataSet.Tables["data"].Rows[i]["PatientID"].ToString());
            }
            this.comboBoxPatient.DataSource = PatientIDs.Distinct().ToList();
            this.comboBoxPatient.SelectedIndex = 0;
            SetcomboBoxTreatment();
        }


        private void SetcomboBoxTreatment()
        {
            string patient = this.comboBoxPatient.SelectedValue.ToString();
            List<string> Sites = new List<string>() { };

            foreach (var dr in this.dataSet.Tables["data"].Select("PatientID='" + patient+"'"))
            {
                Sites.Add(dr.Field<string>("TreatmentID"));
            }
            this.comboBoxTreatment.DataSource = Sites.Distinct().ToList();
            this.comboBoxTreatment.SelectedIndex = 0;
            SetcomboCorrectionByProtocol();
        }

        private void SetcomboCorrectionByProtocol()
        {
            string patient = this.comboBoxPatient.SelectedValue.ToString();
            string site = this.comboBoxTreatment.SelectedValue.ToString();
            List<string> couches = new List<string>() { };

            foreach (var dr in this.dataSet.Tables["data"].Select("PatientID='" + patient + "' AND TreatmentID='"+site+"'"))
            {
                couches.Add(dr.Field<string>("CorrectionByProtocol"));
            }
            this.comboCorrectionByProtocol.DataSource = couches.Distinct().ToList();
            this.comboCorrectionByProtocol.SelectedIndex = 0;
        }


        private void CreateDataset() 
        {
            DataSet CouchTrends = new DataSet("CouchTrends");
            DataTable data = CouchTrends.Tables.Add("data");

            data.Columns.Add("PatientID", Type.GetType("System.String"));
            data.Columns.Add("FirstName", Type.GetType("System.String"));
            data.Columns.Add("LastName", Type.GetType("System.String"));
            data.Columns.Add("TreatmentID", Type.GetType("System.String"));
            data.Columns.Add("Date", typeof(DateTime));
            data.Columns.Add("Time", Type.GetType("System.String"));
            data.Columns.Add("CorrectionByProtocol", Type.GetType("System.String"));
            data.Columns.Add("CorrX", Type.GetType("System.Decimal"));
            data.Columns.Add("CorrY", Type.GetType("System.Decimal"));
            data.Columns.Add("CorrZ", Type.GetType("System.Decimal"));
            data.Columns.Add("CorrXr", Type.GetType("System.Decimal"));
            data.Columns.Add("CorrYr", Type.GetType("System.Decimal"));
            data.Columns.Add("CorrZr", Type.GetType("System.Decimal"));
            this.dataSet = CouchTrends;
        }


        private Decimal ConvertAngle(Decimal angle)
        {
            if (angle >= 0 & angle <= 180)
            {
                return angle;
            }
            else
            {
                return angle - 360;
            }
        }

        private void AddToDataset(Dictionary<string, string> scaninixvi, Dictionary<string, string> scanini)
        {
            var data = this.dataSet.Tables["data"];
            DataRow row = data.NewRow();
            row["PatientID"] = scanini["PatientID"];
            row["FirstName"] = scanini["FirstName"];
            row["LastName"] = scanini["LastName"];
            row["TreatmentID"] = scanini["TreatmentID"];
            //DateTime dt = DateTime.ParseExact(scaninixvi["Date"] + " " + scaninixvi["Time"], "yyyyMMdd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            //row["Date"] = dt.ToString("d");
            //row["Time"] = dt.ToString("t");
            row["Date"] = scaninixvi["Date"];
            row["Time"] = scaninixvi["Time"];
            row["CorrectionByProtocol"] = scaninixvi["CorrectionByProtocol"];
            row["CorrX"] = decimal.Parse(scaninixvi["CorrX"], System.Globalization.CultureInfo.InvariantCulture);
            row["CorrY"] = decimal.Parse(scaninixvi["CorrY"], System.Globalization.CultureInfo.InvariantCulture);
            row["CorrZ"] = decimal.Parse(scaninixvi["CorrZ"], System.Globalization.CultureInfo.InvariantCulture);
            row["CorrXr"] = ConvertAngle(decimal.Parse(scaninixvi["CorrXr"], System.Globalization.CultureInfo.InvariantCulture));
            row["CorrYr"] = ConvertAngle(decimal.Parse(scaninixvi["CorrYr"], System.Globalization.CultureInfo.InvariantCulture));
            row["CorrZr"] = ConvertAngle(decimal.Parse(scaninixvi["CorrZr"], System.Globalization.CultureInfo.InvariantCulture));
            data.Rows.Add(row);
        }


        private List<List<string>> CollectDataForPatient(string PatientFolder)
        {
            // Loop over image directories (Sub-directories of IMAGE) and look for .XVI.INI and .INI files.
            // Group INI files by image
            List<List<string>> finalList = new List<List<string>>() { };

            foreach (string directory in Directory.GetDirectories(PatientFolder))
            {
                List<string> imgFileList = new List<string>() { };
                string reconPath = Path.Combine(directory, "Reconstruction");

                if (Directory.Exists(reconPath))
                {
                    foreach (string fileName in Directory.GetFiles(reconPath))
                    {
                        if (fileName.Contains(".INI"))
                        {
                            imgFileList.Add(fileName);
                        }
                    }
                    if (imgFileList.Count > 0)
                    {
                        finalList.Add(imgFileList);
                    }                   
                }
            }
            return finalList;
        }

        private List<string> ReadLinesFromFile(string file)
        {
            List<string> lines = new List<string>() { };

            using (FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader reader = new StreamReader(fileStream))
                {
                    while (!reader.EndOfStream)
                    {
                        lines.Add(reader.ReadLine());
                    }
                }
            }
            return lines;
        }

        private Dictionary<string, string> ScanINIXVI(string file)
        {
            List<string> lines = ReadLinesFromFile(file).ToList();

            int has_REFERENCE = 0;
            int has_DateTime = 0;
            int has_CorrectionByProtocol = 0;
            int has_ALIGNMENT = 0;
            int has_Aligncorrection = 0;

            string Date = "";
            string Time = "";
            string CorrectionByProtocol = "";
            string CorrX = "";
            string CorrY = "";
            string CorrZ = "";
            string CorrXr = "";
            string CorrYr = "";
            string CorrZr = "";

            for (var i = 0; i < lines.Count; i += 1)
            {
                if (lines[i].Contains("[REFERENCE]"))
                {
                    ++has_REFERENCE;
                    continue;
                }
                if (lines[i].Contains("DateTime") & has_DateTime==0)
                {
                    ++has_DateTime;
                    List<string> dt = lines[i].Replace("DateTime=", "").Replace(" ", "").Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    DateTime dt2 = DateTime.ParseExact(dt.First() + " " + dt.Last(), "yyyyMMdd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                    Date = dt2.ToShortDateString();
                    Time = dt2.ToString("HH:mm");
                    continue;
                }
                if (lines[i].Contains("CorrectionByProtocol"))
                {
                    ++has_CorrectionByProtocol;
                    CorrectionByProtocol = lines[i].Replace("CorrectionByProtocol=", "");
                    continue;
                }
                if (lines[i].Contains("[ALIGNMENT]"))
                {
                    ++has_ALIGNMENT;
                    continue;
                }
                if (lines[i].Contains("Align.correction"))
                {
                    ++has_Aligncorrection;
                    List<string> corr = lines[i].Replace("Align.correction=", "").Replace(" ", "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    CorrX = corr[0];
                    CorrY = corr[1];
                    CorrZ = corr[2];
                    CorrXr = corr[3];
                    CorrYr = corr[4];
                    CorrZr = corr[5];
                    continue;
                }
            }

            if (has_REFERENCE == 1 & has_DateTime == 1 & has_CorrectionByProtocol == 1 & has_ALIGNMENT == 1 & has_Aligncorrection == 1)
            {
                return new Dictionary<string, string>(){
                            {"Date", Date},
                            {"Time", Time},
                            {"CorrectionByProtocol", CorrectionByProtocol},
                            {"CorrX", CorrX },
                            {"CorrY", CorrY },
                            {"CorrZ", CorrZ },
                            {"CorrXr", CorrXr },
                            {"CorrYr", CorrYr },
                            {"CorrZr", CorrZr }
                           };
            }
            else
            {
                return new Dictionary<string, string>(){ };
            }
        }

        private Dictionary<string, string> ScanINI(string file)
        {
            List<string> lines = ReadLinesFromFile(file).ToList();

            int has_IDENTIFICATION = 0;
            int has_PatientID = 0;
            int has_FirstName = 0;
            int has_LastName = 0;
            int has_TreatmentID = 0;

            string PatientID = "";
            string FirstName = "";
            string LastName = "";
            string TreatmentID = "";

            for (var i = 0; i < lines.Count; i += 1)
            {
                if (lines[i].Contains("[IDENTIFICATION]"))
                {
                    ++has_IDENTIFICATION;
                    continue;
                }
                if (lines[i].Contains("PatientID"))
                {
                    ++has_PatientID;
                    PatientID = lines[i].Replace("PatientID=", "");
                    continue;
                }
                if (lines[i].Contains("FirstName"))
                {
                    ++has_FirstName;
                    FirstName = lines[i].Replace("FirstName=", "");
                    continue;
                }
                if (lines[i].Contains("LastName"))
                {
                    ++has_LastName;
                    LastName = lines[i].Replace("LastName=", "");
                    continue;
                }
                if (lines[i].Contains("TreatmentID"))
                {
                    ++has_TreatmentID;
                    TreatmentID = lines[i].Replace("TreatmentID=", "");
                    continue;
                }
            }
            if (has_IDENTIFICATION == 1 & has_PatientID == 1 & has_FirstName == 1 & has_LastName == 1 & has_TreatmentID == 1)
            {
                return new Dictionary<string, string>(){
                            {"PatientID", PatientID},
                            {"FirstName", FirstName},
                            {"LastName", LastName},
                            {"TreatmentID", TreatmentID },
                           };
            }
            else
            {
                return new Dictionary<string, string>() { };
            }
        }


        private void ReadPatientPaths()
        {
            List<string> PatientsPaths_temp = new List<string>() { };
            var lines = File.ReadAllLines(this.PatientsCSVPath);

            for (var i = 0; i < lines.Length; i += 1)
            {
                PatientsPaths_temp.Add(lines[i]);
            }
            this.PatientsPaths = PatientsPaths_temp;
        }

        private void comboBoxPatient_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetcomboBoxTreatment();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // display filtered
            if (this.comboBoxPatient.Items.Count == 0 || this.comboBoxTreatment.Items.Count == 0 || this.comboCorrectionByProtocol.Items.Count == 0)
            {
                return;
            }
            string patient = this.comboBoxPatient.SelectedValue.ToString();
            string site = this.comboBoxTreatment.SelectedValue.ToString();
            string couch = this.comboCorrectionByProtocol.SelectedValue.ToString();

            var dv = this.dataSet.Tables["data"].DefaultView;
            
            dv.RowFilter = "PatientID = '" + patient + "' AND TreatmentID = '" + site + "' AND CorrectionByProtocol='"+couch+"'";
            this.dataGridView1.DataSource = this.dataSet.Tables["data"];
            UpdateCharts();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // display all
            this.dataSet.Tables["data"].DefaultView.RowFilter = string.Empty;
            this.dataGridView1.DataSource = this.dataSet.Tables["data"];
            UpdateCharts();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void UpdateCharts()
        {
            AddPointsChart(chart1, "CorrX");
            AddPointsChart(chart2, "CorrY");
            AddPointsChart(chart3, "CorrZ");
            AddPointsChart(chart4, "CorrXr");
            AddPointsChart(chart5, "CorrYr");
            AddPointsChart(chart6, "CorrZr");
        }

        private void AddPointsChart(Chart chart, string variable)
        {
            ChartArea CA = chart.ChartAreas[0];
            CA.AxisX.ScaleView.Zoomable = true;
            CA.CursorX.AutoScroll = false;
            CA.CursorX.IsUserSelectionEnabled = true;
            CA.CursorX.IsUserEnabled = true;
            CA.AxisX.ScrollBar.IsPositionedInside = true;

            CA.AxisY.ScaleView.Zoomable = true;
            CA.CursorY.AutoScroll = false;
            CA.CursorY.IsUserSelectionEnabled = true;
            CA.CursorY.IsUserEnabled = true;
            CA.AxisY.ScrollBar.IsPositionedInside = true;

            CA.AxisX.ScaleView.MinSize = 10;
            CA.AxisY.ScaleView.MinSize = 0.01;

            CA.CursorY.Interval = 0.01;

            CA.AxisX.IntervalType = DateTimeIntervalType.Days;
            //CA.AxisX.LabelStyle.Format = "g";
            CA.AxisX.IntervalOffset = 1;

            CA.AxisX.MajorGrid.LineColor = Color.Gainsboro;
            CA.AxisY.MajorGrid.LineColor = Color.Gainsboro;

            CA.RecalculateAxesScale();

            chart.Series.Clear();
            chart.Series.Add(variable);
            chart.Series[variable].XValueType = ChartValueType.DateTime;
            chart.Series[variable].XValueMember = "Date";
            chart.Series[variable].YValueMembers = variable;
            chart.Series[variable].ChartType = SeriesChartType.Point;
            chart.Series[variable].MarkerStyle = MarkerStyle.Circle;
            chart.Series[variable].ToolTip = "#VALX \n "+ variable +" = #VAL";
            chart.Legends[0].Docking = Docking.Top;

            DataView view = new DataView(this.dataSet.Tables["data"].DefaultView.ToTable());
            DataTable selected = view.ToTable("data", false, "Date", variable);
            chart.DataSource = selected;
        }

    }
}
