using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using LabJack.LabJackUD;
using System.Management.Automation;
using System.Diagnostics;
using Tesseract;

namespace TFE_managment_soft
{
    public partial class Form1 : Form
    {
        private string errormsg = "";
        //Variables for the U3 labjack + code 
        //private U3 u3;
        private U3_Simple.U3_Simple u3_Simple = new U3_Simple.U3_Simple();

        //Method used to propagate the error messages from the labjack usage
        public void ShowErrorMessage(LabJackUDException e)
        {
            Console.Out.WriteLine("Error: " + e.ToString());
            if (e.LJUDError > U3.LJUDERROR.MIN_GROUP_ERROR)
            {
                Console.ReadLine(); // Pause for the user
                Environment.Exit(-1);
            }
        }
        
        //Variable holding the command number that should be printed on the running command
        private int controleNumberStored;
        private int commandNumberCartomills;
        
        private int counter;
        //Variable holding the directory with the file where the comparison code is stored
        //private string directoryPathData = @"C:\Data\Controleur\192.168.1.50\SD2\cv-x\result\SD1_000\";
        //private string directoryPathImage = @"C:\Data\Controleur\192.168.1.50\SD2\cv-x\image\SD1_000\";
        
        static string directoryPathImage = @"C:\Data\Controleur\192.168.1.50\SD2\cv-x\image\SD1_001\CAM1";
        static string directoryPathImageStore = @"C:\Data\Controleur\ImagesTraitees\";
        
        //Variable holding the latest image taken
        
        private string pathToImageToPrint = "";

        //Variable used to store the numbers about the comparison
        private int counterLineCompared; //Total number of lines read and compared so far
        private int counterComparisonError; //Total number of codes that didn't match the stored command number

        private int counterNumberCartons; //Total number of cartons in the actual command

        private int errorIndex; //Index at which an error occured
        private int scannedAfterError; //Number of cartons scanned since an error occured

        //Variables used to know what state the comparison in in
        private bool continueComparison = false;
        private bool errorInComparison;// = false;
        private bool commandFinished = false;
        

        //TextBox used to know which textbox the user when to input number in
        TextBox selTB = null;

        //Initialisation of the different elements used during the comparison
        public Form1()
        {
            InitializeComponent();
            backgroundWorker1.DoWork += backgroundWorker1_DoWork;
            backgroundWorker1.ProgressChanged += backgroundWorker1_ProgressChanged;
            backgroundWorker1.RunWorkerCompleted += backgroundWorker1_RunWorkerCompleted;
            richTextBox1.Font = new Font("Times New Roman", 96.0f);
            pictureBox1.Hide();
            richTextBox1.Hide();
            textBox6.Hide();


            textBox1.Enter += tb_Enter;
            textBox2.Enter += tb_Enter;

            
        }
        //Initialisation of the two writable textBoxes
        ~Form1()
        {
            textBox1.Enter -= tb_Enter;
            textBox2.Enter -= tb_Enter;
        }
        private void tb_Enter(object sender, EventArgs e)
        {
            selTB = (TextBox)sender;
        }

        //Form elements initialization
        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }
        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }
        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }
        private void textBox5_TextChanged(object sender, EventArgs e)
        {

        }
        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        //Button1 click managment
        //Function : Start the comparison between the entered command number and the scanned ones
        //Function : Stop the comparison/command
        //Function : Continue the command after an error
        //Function : Prepare the programme for a new command
        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text.Equals("Démarrer la commande"))
            {
                counterComparisonError = 0;
                counterNumberCartons = 0;
                errorIndex = 0;

                textBox6.Hide();
                pictureBox1.Show();
                richTextBox1.Show();
                richTextBox1.Clear();
                if (Int32.TryParse(textBox1.Text, out controleNumberStored) && Int32.TryParse(textBox2.Text, out commandNumberCartomills))
                {
                    try
                    {
                        errorInComparison = false;
                        commandFinished = false;
                        continueComparison = true;
                        button1.Text = "Terminer la commande";
                        backgroundWorker1.RunWorkerAsync();
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Problème lors de la comparaison, veuillez relancer le programme. Code d'erreur 001");
                    }
                }
                else
                {
                    MessageBox.Show("Veuillez entrer un numéro de commande et un code de comparaison");
                }
            }

            else if (button1.Text.Equals("Continuer la commande"))
            {
                textBox6.Hide();
                try
                {
                    errorIndex = 0;
                    scannedAfterError = 0;
                    errorInComparison = false;
                    button1.Text = "Terminer la commande";
                }
                catch (Exception)
                {
                    MessageBox.Show("Problème lors de la comparaison, veuillez relancer le programme. Code d'erreur 002");
                }
            }

            else if (button1.Text.Equals("Terminer la commande"))
            {
                textBox6.Hide();
                pictureBox1.Hide();
                richTextBox1.Hide();

                backgroundWorker1.CancelAsync();
                commandFinished = true;
                continueComparison = false;
                string errorPercentage = ((double)counterComparisonError / (double)counter * 100).ToString("#.##");

                string[] lines = new string[6];
                lines[0] = "Numéro de commande : " + commandNumberCartomills.ToString();
                lines[1] = "code de contrôle : " + controleNumberStored.ToString();
                lines[2] = "Nombre de cartons scannés : " + counterNumberCartons.ToString();
                lines[3] = "Nombre de cartons mauvais : " + counterComparisonError.ToString();
                lines[4] = ($"{errorPercentage} % des cartons scannés n'avaient pas le bon code de contrôle");
                lines[5] = DateTime.Now.ToString();
                string outFileName = @"C:\Data\Sorties\Statistiques_Commandes\" + commandNumberCartomills.ToString() + ".txt";
                System.IO.File.AppendAllLines(outFileName, lines);
                textBox6.Text = "Commande temrinée" + "\r\n";
                textBox6.Text += "Numéro de commande : " + commandNumberCartomills.ToString() + "\r\n";
                textBox6.Text += "code de contrôle : " + controleNumberStored.ToString() + "\r\n";
                textBox6.Text += "Nombre de cartons scannés : " + counterNumberCartons.ToString() + "\r\n";
                textBox6.Text += "Nombre de cartons mauvais : " + counterComparisonError.ToString() + "\r\n";

                button1.Text = "Démarrer la commande";
                textBox3.Text = "";
                textBox4.Text = "";
                richTextBox1.Text = "";


                using (PowerShell PowerShellInstance = PowerShell.Create())
                {
                    PowerShellInstance.AddScript(@"Remove-Item -path C:\Data\Controleur\* -Include *.bmp -Recurse");
                    PowerShellInstance.Invoke();
                }
            }
            else if (button1.Text.Equals("Démarrer une nouvelle commande"))
            {
                textBox6.Hide();
                pictureBox1.Hide();
                richTextBox1.Hide();
                controleNumberStored = 0;
                commandNumberCartomills = 0;
                pathToImageToPrint = "";
                counterComparisonError = 0;
                counterNumberCartons = 0;
                errorIndex = 0;
                button1.Text = "Démarrer la commande";
            }
        }
        
        private void button3_Click(object sender, EventArgs e)
        {
            //Completely reset the session values to start a new command.
            Application.Restart();
        }

        //Methods linked to the backgroundWorker. It allows the comparison to run in a separated thread
        //This allows the ui to update in real time while the comparison keeps going on until stopped

        //DoWork : Actual code that runs when the comparison starts + check if it needs to stop.
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {

            //Variable to hold the count of images and to rename them
            counter = 0;

            int progressCount = 0;
            do
            {

                System.Threading.Thread.Sleep(500);
                //Variable holding the latest image taken
                string pathToImage = "";

                string code_extracted = "";

                

                System.Threading.Thread.Sleep(400);
                var dir2 = new DirectoryInfo(directoryPathImage);
                try
                {
                    foreach (var file2 in dir2.EnumerateFiles("*.bmp"))
                    {
                        pathToImage = dir2.ToString() + @"\" + file2;
                        
                        code_extracted = Compare2(pathToImage);
                        errormsg = code_extracted;
                        //Delete the image file used to extract the code
                        pathToImageToPrint = directoryPathImageStore + counter.ToString() + ".bmp";
                        MoveFile(pathToImage, pathToImageToPrint);
                        counter++;
                    }
                }
                catch (Exception)
                {

                }
                
                //Compare();
                if (backgroundWorker1.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }
                backgroundWorker1.ReportProgress(++progressCount);
            } while (continueComparison);
        }

        //ProgressChanged : Check whether there has been a progress change and update the UI accordingly.
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            textBox5.Text = errormsg;
            try
            {
                pictureBox1.Image = Image.FromFile(pathToImageToPrint);
            }
            catch (Exception)
            {
                
            }
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            if (errorInComparison)
            {
                u3_Simple.outputHigh();
                textBox6.Show();
                button1.Text = "Continuer la commande";
                textBox6.Text = "Erreur lors de la comparaison !" + "\r\n";
                textBox6.Text += "Nombre de cartons scannés depuis l'erreur : " + scannedAfterError.ToString() + "\r\n";
                textBox6.Text += "Veuillez les retirer avant de continuer la commande. \r\n";
                richTextBox1.ForeColor = Color.Red;
                richTextBox1.Text = "NOK";
            }
            else
            {
                u3_Simple.outputLow();
                richTextBox1.ForeColor = Color.Green;
                if (counterNumberCartons != 0)
                {
                    richTextBox1.Text = "OK";
                }
                else
                {
                    pictureBox1.Image = null;
                }
            }
            textBox3.Text = ($"{counterNumberCartons} cartons ont été scannés et comparés.");
            textBox4.Text = ($"{counterComparisonError} cartons n'ont pas le bon numéro de commande.");

        }

        //RunWorkerCompleted : Code to run when the background thread stops
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (commandFinished)
            {
                button1.Text = "Démarrer une nouvelle commande";
                textBox3.Text = "";
                textBox4.Text = "";
            }
        }

        //Method that will read a file and compare the code stored in it with the code indicated by the machine conductor
        private string Compare2(string path)
        {
            string extractedCode = RecognizeText(path);
            if (!extractedCode.Contains(controleNumberStored.ToString()))
            {
                counterComparisonError++;
                errorInComparison = true;
                errorIndex = counterLineCompared;
                counterNumberCartons++;
                counterLineCompared++;
                if (errorIndex == 0)
                {
                    errorIndex = counterNumberCartons;
                }
                else
                {
                    scannedAfterError++;
                }
                return extractedCode;
            }
            else
            {
                counterNumberCartons++;
                counterLineCompared++;
                if (errorInComparison)
                {
                    scannedAfterError++;
                }
                return extractedCode;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {

        }

        //Buttons composing the numpad+ret on screen 
        //0
        private void button14_Click(object sender, EventArgs e)
        {
            if (selTB != null) selTB.Focus();
            selTB.Text += "0";
        }
        //1
        private void button2_Click(object sender, EventArgs e)
        {
            if (selTB != null) selTB.Focus();
            selTB.Text += "1";
        }
        //2
        private void button4_Click(object sender, EventArgs e)
        {
            if (selTB != null) selTB.Focus();
            selTB.Text += "2";
        }
        //3
        private void button5_Click(object sender, EventArgs e)
        {
            if (selTB != null) selTB.Focus();
            selTB.Text += "3";
        }
        //4
        private void button8_Click(object sender, EventArgs e)
        {
            if (selTB != null) selTB.Focus();
            selTB.Text += "4";
        }
        //5
        private void button7_Click(object sender, EventArgs e)
        {
            if (selTB != null) selTB.Focus();
            selTB.Text += "5";
        }
        //6
        private void button6_Click(object sender, EventArgs e)
        {
            if (selTB != null) selTB.Focus();
            selTB.Text += "6";
        }
        //7
        private void button11_Click(object sender, EventArgs e)
        {
            if (selTB != null) selTB.Focus();
            selTB.Text += "7";
        }
        //8
        private void button10_Click(object sender, EventArgs e)
        {
            if (selTB != null) selTB.Focus();
            selTB.Text += "8";
        }
        //9
        private void button9_Click(object sender, EventArgs e)
        {
            if (selTB != null) selTB.Focus();
            selTB.Text += "9";
        }
        //Retour
        private void button12_Click(object sender, EventArgs e)
        {
            if (selTB != null) selTB.Focus();
            if (selTB.Text.Length > 0)
            {
                selTB.Text = selTB.Text.Substring(0, selTB.Text.Length - 1);
            }
        }
        
        //Method using the tesseract module in order to extract the code from the images
        static public string RecognizeText(string path)
        {
            try
            {
                using (var engine = new TesseractEngine(@"C:\Data\Tesseract\tessdata", "eng", EngineMode.Default))
                {
                    engine.SetVariable("tessedit_char_whitelist", "01234567890");
                    using (var img = Pix.LoadFromFile(path))
                    {
                        using (var page = engine.Process(img))
                        {
                            var text = page.GetText();
                            return text;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                string ret = "Unexpected Error: " + e.Message + "\n Details: " + e.ToString();
                return ret;
            }
        }

        //Method used to move files from one place of a computer to another and that waits for the copy to finish before ending th method
        static private async void MoveFile(string sourceFile, string destinationFile)
        {
            try
            {
                using (FileStream sourceStream = File.Open(sourceFile, FileMode.Open))
                {
                    using (FileStream destinationStream = File.Create(destinationFile))
                    {
                        await sourceStream.CopyToAsync(destinationStream);
                        sourceStream.Close();
                        File.Delete(sourceFile);
                    }
                }
            }
            catch (IOException ioex)
            {

            }
            catch (Exception ex)
            {

            }
        }

    }
}
namespace U3_Simple
{
    class U3_Simple
    {
        // our U3 variable
        public U3 u3;

        // If error occured print a message indicating which one occurred. If the error is a group error (communication/fatal), quit
        public void ShowErrorMessage(LabJackUDException e)
        {
            MessageBox.Show(e.ToString());
        }

        public void outputHigh()
        {
            try
            {
                //Open the first found LabJack U3.
                u3 = new U3(LJUD.CONNECTION.USB, "0", true); // Connection through USB

                //Start by using the pin_configuration_reset IOType so that all
                //pin assignments are in the factory default condition.
                LJUD.ePut(u3.ljhandle, LJUD.IO.PIN_CONFIGURATION_RESET, 0, 0, 0);


                //First some configuration commands.  These will be done with the ePut
                //function which combines the add/go/get into a single call.

                //Configure FIO0-FIO3 as analog, all else as digital.  That means we
                //will start from channel 0 and update all 16 flexible bits.  We will
                //pass a value of b0000000000001111 or d15.
                //LJUD.ePut(u3.ljhandle, LJUD.IO.PUT_ANALOG_ENABLE_PORT, 0, 15, 16);

                //The following commands will use the add-go-get method to group
                //multiple requests into a single low-level function.

                //Set DAC0 to 5 volts.
                LJUD.AddRequest(u3.ljhandle, LJUD.IO.PUT_DAC, 0, 5, 0, 0);
                LJUD.AddRequest(u3.ljhandle, LJUD.IO.PUT_DAC, 1, 0, 0, 0);

                //Set digital output FIO2 to output-high.
                //LJUD.AddRequest(u3.ljhandle, LJUD.IO.PUT_DIGITAL_BIT, 1, 5, 0, 0);
                LJUD.GoOne(u3.ljhandle);

                //Set digital output FIO3 to output-low.
                //LJUD.AddRequest(u3.ljhandle, LJUD.IO.PUT_DIGITAL_BIT, 5, 0, 0, 0);
            }
            catch (LabJackUDException e)
            {
                ShowErrorMessage(e);
            }
        }
        public void outputLow()
        {
            try
            {
                //Open the first found LabJack U3.
                u3 = new U3(LJUD.CONNECTION.USB, "0", true); // Connection through USB

                //Start by using the pin_configuration_reset IOType so that all
                //pin assignments are in the factory default condition.
                LJUD.ePut(u3.ljhandle, LJUD.IO.PIN_CONFIGURATION_RESET, 0, 0, 0);


                //First some configuration commands.  These will be done with the ePut
                //function which combines the add/go/get into a single call.

                //Configure FIO0-FIO3 as analog, all else as digital.  That means we
                //will start from channel 0 and update all 16 flexible bits.  We will
                //pass a value of b0000000000001111 or d15.
                //LJUD.ePut(u3.ljhandle, LJUD.IO.PUT_ANALOG_ENABLE_PORT, 0, 15, 16);

                //The following commands will use the add-go-get method to group
                //multiple requests into a single low-level function.

                //Set DAC0 to 0 volts.
                LJUD.AddRequest(u3.ljhandle, LJUD.IO.PUT_DAC, 0, 0, 0, 0);
                LJUD.AddRequest(u3.ljhandle, LJUD.IO.PUT_DAC, 1, 5, 0, 0);

                //Set digital output FIO2 to output-high.
                //LJUD.AddRequest(u3.ljhandle, LJUD.IO.PUT_DIGITAL_BIT, 1, 0, 0, 0);

                LJUD.GoOne(u3.ljhandle);
                //Set digital output FIO3 to output-low.
                //LJUD.AddRequest(u3.ljhandle, LJUD.IO.PUT_DIGITAL_BIT, 1, 0, 0, 0);
            }
            catch (LabJackUDException e)
            {
                ShowErrorMessage(e);
            }
        }
        //public void performActions()
        //{
        //    double dblDriverVersion;
        //    LJUD.IO ioType = 0;
        //    LJUD.CHANNEL channel = 0;
        //    double dblValue = 0;
        //    double Value0 = 9999, Value1 = 9999, Value2 = 9999;
        //    double ValueDIBit = 9999, ValueDIPort = 9999, ValueCounter = 9999;

        //    // Variables to satisfy certain method signatures
        //    int dummyInt = 0;
        //    double dummyDouble = 0;

        //    //Read and display the UD version.
        //    dblDriverVersion = LJUD.GetDriverVersion();
        //    Console.Out.WriteLine("UD Driver Version = {0:0.000}\n\n", dblDriverVersion);

        //    try
        //    {
        //        //Open the first found LabJack U3.
        //        u3 = new U3(LJUD.CONNECTION.USB, "0", true); // Connection through USB

        //        //Start by using the pin_configuration_reset IOType so that all
        //        //pin assignments are in the factory default condition.
        //        LJUD.ePut(u3.ljhandle, LJUD.IO.PIN_CONFIGURATION_RESET, 0, 0, 0);


        //        //First some configuration commands.  These will be done with the ePut
        //        //function which combines the add/go/get into a single call.

        //        //Configure FIO0-FIO3 as analog, all else as digital.  That means we
        //        //will start from channel 0 and update all 16 flexible bits.  We will
        //        //pass a value of b0000000000001111 or d15.
        //        LJUD.ePut(u3.ljhandle, LJUD.IO.PUT_ANALOG_ENABLE_PORT, 0, 15, 16);

        //        //Set the timer/counter pin offset to 7, which will put the first
        //        //timer/counter on FIO7.
        //        LJUD.ePut(u3.ljhandle, LJUD.IO.PUT_CONFIG, LJUD.CHANNEL.TIMER_COUNTER_PIN_OFFSET, 7, 0);

        //        //Enable Counter1 (FIO7).
        //        LJUD.ePut(u3.ljhandle, LJUD.IO.PUT_COUNTER_ENABLE, (LJUD.CHANNEL)1, 1, 0);


        //        //The following commands will use the add-go-get method to group
        //        //multiple requests into a single low-level function.

        //        //Request a single-ended reading from AIN0.
        //        LJUD.AddRequest(u3.ljhandle, LJUD.IO.GET_AIN, 0, 0, 0, 0);

        //        //Request a single-ended reading from AIN1.
        //        LJUD.AddRequest(u3.ljhandle, LJUD.IO.GET_AIN, 1, 0, 0, 0);

        //        //Request a reading from AIN2 using the Special range.
        //        LJUD.AddRequest(u3.ljhandle, LJUD.IO.GET_AIN_DIFF, 2, 0, 32, 0);

        //        //Set DAC0 to 3.5 volts.
        //        LJUD.AddRequest(u3.ljhandle, LJUD.IO.PUT_DAC, 0, 3.5, 0, 0);

        //        //Set digital output FIO4 to output-high.
        //        LJUD.AddRequest(u3.ljhandle, LJUD.IO.PUT_DIGITAL_BIT, 4, 1, 0, 0);

        //        //Read digital input FIO5.
        //        LJUD.AddRequest(u3.ljhandle, LJUD.IO.GET_DIGITAL_BIT, 5, 0, 0, 0);

        //        //Read digital inputs FIO5 through FIO6.
        //        LJUD.AddRequest(u3.ljhandle, LJUD.IO.GET_DIGITAL_PORT, 5, 0, 2, 0);

        //        //Request the value of Counter1 (FIO7).
        //        LJUD.AddRequest(u3.ljhandle, LJUD.IO.GET_COUNTER, 1, 0, 0, 0);
        //    }
        //    catch (LabJackUDException e)
        //    {
        //        ShowErrorMessage(e);
        //    }
        //    bool requestedExit = false;
        //    while (!requestedExit)
        //    {
        //        try
        //        {
        //            //Execute the requests.
        //            LJUD.GoOne(u3.ljhandle);

        //            //Get all the results.  The input measurement results are stored.  All other
        //            //results are for configuration or output requests so we are just checking
        //            //whether there was an error.
        //            LJUD.GetFirstResult(u3.ljhandle, ref ioType, ref channel, ref dblValue, ref dummyInt, ref dummyDouble);
        //        }
        //        catch (LabJackUDException e)
        //        {
        //            ShowErrorMessage(e);
        //        }

        //        bool finished = false;
        //        while (!finished)
        //        {
        //            switch (ioType)
        //            {

        //                case LJUD.IO.GET_AIN:
        //                    switch ((int)channel)
        //                    {
        //                        case 0:
        //                            Value0 = dblValue;
        //                            break;
        //                        case 1:
        //                            Value1 = dblValue;
        //                            break;
        //                    }
        //                    break;

        //                case LJUD.IO.GET_AIN_DIFF:
        //                    Value2 = dblValue;
        //                    break;

        //                case LJUD.IO.GET_DIGITAL_BIT:
        //                    ValueDIBit = dblValue;
        //                    break;

        //                case LJUD.IO.GET_DIGITAL_PORT:
        //                    ValueDIPort = dblValue;
        //                    break;

        //                case LJUD.IO.GET_COUNTER:
        //                    ValueCounter = dblValue;
        //                    break;

        //            }
        //            try { LJUD.GetNextResult(u3.ljhandle, ref ioType, ref channel, ref dblValue, ref dummyInt, ref dummyDouble); }
        //            catch (LabJackUDException e)
        //            {
        //                // If we get an error, report it.  If the error is NO_MORE_DATA_AVAILABLE we are done
        //                if (e.LJUDError == U3.LJUDERROR.NO_MORE_DATA_AVAILABLE)
        //                    finished = true;
        //                else
        //                    ShowErrorMessage(e);
        //            }
        //        }

        //        Console.Out.WriteLine("AIN0 = {0:0.###}\n", Value0);
        //        Console.Out.WriteLine("AIN1 = {0:0.###}\n", Value1);
        //        Console.Out.WriteLine("AIN2 = {0:0.###}\n", Value2);
        //        Console.Out.WriteLine("FIO5 = {0:0.###}\n", ValueDIBit);
        //        Console.Out.WriteLine("FIO5-FIO6 = {0:0.###}\n", ValueDIPort);  //Will read 3 (binary 11) if both lines are pulled-high as normal.
        //        Console.Out.WriteLine("Counter1 (FIO7) = {0:0.###}\n", ValueCounter);

        //        Console.Out.WriteLine("\nPress Enter to go again or (q) to quit\n");
        //        requestedExit = Console.ReadLine().Equals("q");
        //    }
        //}
    }

}
