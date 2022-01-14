using System;
//drive format nabashad error mide
//baad az taeen folder, agar folder delete shavad, va start konim error mide, pas daghighan bayad ghabl az start check shavd folder existence
//dar in halat close kardane application ham error mide (va har ja ke folderdialog.selected pth estefade shode bashad.)
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Resources;
//application.exit ta yek thread suspend ,run va ,... bashe kharej nemishe va form baste mishe vali hanooz process bazeh!
namespace WindowsFormsApplication1
/*
 age read only bood rewrite ya delete momkene nemishe yek file (folder chi delete mishe)
    Set the files attributes to normal in case it's read-only.
    File.SetAttributes(filename, FileAttributes.Normal);
     
 in software faghat free space ra wipe mikone (che dakhele yek drive che yek folder ra bedim kole free space ann drive marbooteh ra wipe mikone)
 pass bayad ghablesh drive ba dast format shavad. ya software joori neveshte beshe ke aval delete kone hame ra. ham bayad betoone free space ba hefze file ha, ham  file hash ra ham wipe konad.
     
     
 in software felan yek file ra wipe nemikone ke hey roosh benevisim. aval bayad ba dast delete konim baad kole free space ra wipe konim.
 data time created va attributes ha ham bayad avaz shavad.
 
  
 folder ra wipe koni che, file hash ra wipe sepas khode folder ra rename va delete
 */




    //kari kardim ke hade aghal 10 MB free space bekhad.chon  age kheili kamtar bashe progress bar error mideh.
{   //az free space komak begiri tedad file kamtari misazi
    //block size chi ,tah mande block hanooz moheme pas mezrabi az block bashad size behtare va fit tar!, ba che size format shavad?
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        List<string> files = new List<string>();

        bool is_pause = false;












        #region wipe

        void wipe(string path, int option)
        {
            
            //Random rnd = new Random();
            int file_size = 1 * 1024 * 1024 ;//1MB faster than 1 GB??   !!!progress bar faghat baraye size asli jeloomire na riztar ha
            int name = 0;
            BinaryWriter br = null;
            byte[] buffer = null;
            Stream str = null;


           
            while (true)
            {
                try
                {
                    name++;

                    str = File.Create(path + "\\" + name + ".xbinx");
                    files.Add(path + "\\" + name + ".xbinx");

                    br = new BinaryWriter(str);
                    buffer = new byte[file_size];//consumes high memory


                    if (option == 0)
                    {
                        //for (int i = 0; i < buffer.Length; i++) { buffer[i] = 0;//default in .NET is zero }
                    }
                    if (option == 1)
                    {
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            buffer[i] = 255;
                        }
                    }
                    if (option == 2)
                    {//random
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            buffer[i] = 170;//10101010 static random
                        }   //(byte)rnd.Next(0, 256); }//random consumes cpu
                    }

                    br.Write(buffer);
                    //br.Flush();   chera in error mideh????????????????????

                    br.Close();


                    //only for size asli avaliye jeloo mire na riz ha
                    if(file_size >= 1 * 1024 * 1024){//1MB
                    Invoke(new DelegateIncrementCounter(() => //++ after 100% write before catch only
                    {
                        progressBar1.Value++;

                        label3.Text = (((progressBar1.Value * 100) / progressBar1.Maximum)).ToString() + "%";
                        this.Refresh();
                    }
                   ));
                    }

                    // br.Dispose();

                }
                catch/*(Exception exception)*/ {
                    br.Close();
                    //br.Dispose();
                    File.Delete(path + "\\" + name + ".xbinx");
                   
                   // files.Remove(path + "\\" + name + ".xbinx");
                    name--;

                    //now remained free space is smaller than current file_size 
                    if (file_size == 1024) { break; }

                    file_size = file_size / 2;

                    // MessageBox.Show(  exception.Message.ToString());

                }



            } 

        }
        #endregion


        #region clean_dumpe_files
        void clean_dumpe_files(string path)
        {
            
            DirectoryInfo di = new DirectoryInfo(path);
            FileInfo[] files = di.GetFiles("*.xbinx");
            //DirectoryInfo[] folders = di.GetDirectories();


            //-------------------------------------------------

            //foreach (DirectoryInfo fol in folders)
            //{
            //    clean_dumpe_files(path + "\\" + fol.Name);

            //}


            foreach (FileInfo fil in files)
            {


                if (fil.Extension == ".xbinx")
                {
                    File.Delete(path + "\\" + fil.Name);
                }




            }

        }
        #endregion
       
        private void Form1_Load(object sender, EventArgs e)
        {
            
            button2.Enabled = false;
            button1.Enabled = false;
            label1.Text = "Ready";

            checkBox1.Checked = true;//000 active
            button3.Enabled = false;

            progressBar1.Enabled = false;
            label3.Visible = false;//percent

            this.Refresh();

            /***/folderBrowserDialog1.SelectedPath = "e:\\wipe";
        }


        delegate void DelegateIncrementCounter();

        int number_of_writes = 0;
        #region  start
        void start()
        {
            number_of_writes = (int)numericUpDown1.Value;
            if (number_of_writes < 1) { MessageBox.Show("Finished"); return; }

            /////////////////////////
            DirectoryInfo df = new DirectoryInfo(folderBrowserDialog1.SelectedPath);
            DriveInfo drive_info = new DriveInfo(df.Root.ToString());
            long freespace = drive_info.AvailableFreeSpace;//in bytes


            Invoke(new DelegateIncrementCounter(() =>
            {

                label1.Text = "Working...";
                checkBox1.Enabled = checkBox2.Enabled = checkBox3.Enabled = false;
                numericUpDown1.Enabled = false;

                button4.Enabled = button1.Enabled = false;
                button2.Enabled = true;//active cancel
                button3.Enabled = true;//active pause
                progressBar1.Enabled = true;
                label3.Visible = true;
                progressBar1.Value = 0;
                this.Refresh();

            }
          ));  

            

            
            ///////////////////////////////////////
            //file_count mostaghel az number_of_writes va tedade write_mode ast, 
            int file_count/*or free_space in int */= (int)(freespace / (1024 * 1024));//in MB//baraye har wipe tedad filhaye 1 MB ke neveshte mishe(joz riz ha)//ashari ash mipare dar cast, pass tedad filehaye  kamtar  az 1MB ra nemige. vali jame in file haye riz hadeaksar 1 MB ast.
            //dar sade progress bar faghat ba tedaad file haye 1MB kar dare
            
            
            //////////////
            int total_required_steps = 0;
            if (checkBox1.Checked) { total_required_steps += file_count; }
            if (checkBox2.Checked) { total_required_steps += file_count; }
            if (checkBox3.Checked) { total_required_steps += file_count; }
            
            //////////////
            Invoke(new DelegateIncrementCounter(() =>
            {
                progressBar1.Maximum = total_required_steps * number_of_writes;  this.Refresh();                
            }
           ));           
            ////////////////////////////////////////

            for (int i = 0; i < number_of_writes; i++ )//bad har seri delete nemikone rooye hamn ha rewrite mikone, dast akhar delete mikone, har sei ham shamele 000,111,101 tebghe voroodi karbar ast. yani yek bar daste aznjam mishe sepas, 2bare 
            {  //start of number of times to write

                if (checkBox1.Checked)
                {
                   // try
                    {

                        wipe(folderBrowserDialog1.SelectedPath, 0);


                    }
                   // catch
                    {
                        //button4.Enabled = button1.Enabled = true;
                        //MessageBox.Show("ERROR.");
                    }
                }



                if (checkBox2.Checked)
                {
                   // try
                    {

                        wipe(folderBrowserDialog1.SelectedPath, 1);

                    }
                   // catch
                    {
                        //button4.Enabled = button1.Enabled = true;
                        //MessageBox.Show("ERROR.");
                    }


                }


                if (checkBox3.Checked)
                {
                   // try
                    {

                        wipe(folderBrowserDialog1.SelectedPath, 2);

                    }
                  //  catch
                    {
                        //button4.Enabled = button1.Enabled = true;
                        //MessageBox.Show("ERROR.");
                    }


                }

            } //end of number of writes


            Invoke(new DelegateIncrementCounter(() =>
            {
                label1.Text = "Finished";
                checkBox1.Enabled = checkBox2.Enabled = checkBox3.Enabled = true;
                numericUpDown1.Enabled = true;

                button4.Enabled = button1.Enabled = true;
                button2.Enabled = false;//deactive cancel
                button3.Enabled = false;//deactive pause

                    
                    
                    //this.Refresh();              
                
            }
           ));
            
            clean_dumpe_files(folderBrowserDialog1.SelectedPath);

            MessageBox.Show("Finished");

            Invoke(new DelegateIncrementCounter(() =>
            {

                label3.Visible = false;//percent
                progressBar1.Enabled = false;
                progressBar1.Value = 0;
            }
          ));
            
            
        }

        #endregion



        ThreadStart t_start;
        Thread _Thread;



        #region start_delete_thread
        void start_delete_thread() {
            

            DirectoryInfo df = new DirectoryInfo(folderBrowserDialog1.SelectedPath);
            DriveInfo drive_info = new DriveInfo(df.Root.ToString());
            long freespace = drive_info.AvailableFreeSpace;//in bytes

            if (freespace < 10 * 1024 * 1024)//in ja 2bare check mikonim,chon momkene ba haman path mojood, disk space avaz beshe.!
            {
                MessageBox.Show("The operation requires at least 10 MB free disk space.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }



            //if (!Directory.Exists(folderBrowserDialog1.SelectedPath))
            //{
            //    MessageBox.Show("Folder doesn't exist.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    return;
            //}


            DialogResult result = MessageBox.Show("Are you sure to safely delete data?", "Warning!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.No) return;



            //folderBrowserDialog1.SelectedPath = "e:\\wipe";
            t_start = new ThreadStart(start);
            _Thread = new Thread(start);
            // start();
            // return;
            _Thread.IsBackground = false;
            _Thread.Start();
            // _Thread2.Join();
        
        
        
        }

        #endregion
        private void button1_Click(object sender, EventArgs e){            
            start_delete_thread();
        }

        #region select_path
        void select_path()
        {

            folderBrowserDialog1.ShowDialog();

            if (folderBrowserDialog1.SelectedPath == "")
            {
                button1.Enabled = false;
                //label1.Text = "";
                return;
            }
            else
            {

                label2.Text = folderBrowserDialog1.SelectedPath;
                button1.Enabled = true;

                //label1.Text = "Path selected";
                return;
            }
        }

        #endregion
        private void button4_Click(object sender, EventArgs e)
        {
            select_path();
        }

        #region cancel_with_gui_update
        void cancel_with_gui_update() {

            button2.Enabled = false;


            button3.Enabled = false;
            label1.Text = "Canceling...";
            this.Refresh();
            ////////////
            cancel();
            ////////////
            button3.Text = "Pause";



            button4.Enabled = button1.Enabled = true;//path  ,start
            label1.Text = "Ready";
            checkBox1.Enabled = checkBox2.Enabled = checkBox3.Enabled = true;
            numericUpDown1.Enabled = true;

            progressBar1.Enabled = false;
            progressBar1.Value = 0;
            label3.Visible = false;//percent

            this.Refresh();

        }
        #endregion


        #region cancel
        void cancel()
        {//without gui update
           

            try
            {
                if (this._Thread.ThreadState != ThreadState.Suspended)//yani runnig, runnig ra tashkhis nemide.fekr konam runing yani daghighan dar ejra na cpu nadashte bashad ke in barabar ba zendeh boodane thread che running che wait time(na suspend) ke ma mikhahim nist, pas running be dad ma nemikhore.
                {
                    //MessageBox.Show("1  suspended");
                    _Thread.Abort(); //abort from runnnig //Abort always draws an exception
                    // while (this._Thread.ThreadState = ThreadState.Aborted) ; //takes time to Abort();


                    _Thread.Join();//DAR Abort() HAYE nagahani FILE FOPEN MOMAND DAR THREAD VA TABE CLEAN_FILES()
                    //nemitonest delete konad migoft "uses by other process" vali ba join() baad az abort()
                    //moshkel hal shod! fekr konam join() resource thread ra azad mikone che karesh tamam be she che
                    //abort shode bashe.
                
                
                }


                if (this._Thread.ThreadState == ThreadState.Suspended)
                {

                     //MessageBox.Show("suspended");
                    _Thread.Resume();
                    _Thread.Abort();//?????///abort from suspended makes error!!!!so we resume it again then abort  
                    // while (this._Thread.ThreadState == ThreadState.Suspended) ; //takes time to Abort();


                    _Thread.Join();//DAR Abort() HAYE nagahani FILE FOPEN MOMAND DAR THREAD VA TABE CLEAN_FILES()
                    //nemitonest delete konad migoft "uses by other process" vali ba join() baad az abort()
                    //moshkel hal shod! fekr konam join() resource thread ra azad mikone che karesh tamam be she che
                    //abort shode bashe.
                    


                }
            }
            catch
            {


            }
            




            // while (this._Thread.IsAlive) ;
            clean_dumpe_files(folderBrowserDialog1.SelectedPath);

            is_pause = false;//chon ba cancel dige nemishe continue kard, 2bare az pause shroo mishe
           


        }
        #endregion
        private void button2_Click(object sender, EventArgs e)
        {

            cancel_with_gui_update();
        }



        #region pause_contiue
        void pause_contiue(){
    
    if(is_pause==false){

                button3.Enabled = false;
                button2.Enabled = false;
                label1.Text = "Pausing...";
                this.Refresh();

                is_pause = true;  

                //if (this._Thread.ThreadState==ThreadState.Running )
                {
                   
                    this._Thread.Suspend();
                   
                   // while (this._Thread.ThreadState != ThreadState.Suspended) ; //takes time to Suspended();
                   
                }

                label1.Text = "Paused";
                button3.Text = "Continue";
                button2.Enabled = true;
                button3.Enabled = true;
                this.Refresh();

                return;
            }

            /////////////////////////////////////////////////////////////
            if (is_pause == true)
            {
                button2.Enabled = false;
                button3.Enabled = false;
                label1.Text = "Continuing...";
                is_pause = false;
                
                //if ( this._Thread.ThreadState == ThreadState.Suspended)
                {
                    
                    this._Thread.Resume();
                   
                  //  while (this._Thread.ThreadState != ThreadState.Running) ; //takes time to Suspended();
                    
                }

                label1.Text = "Working...";
                button3.Text = "Pause";
                button2.Enabled = true;
                button3.Enabled = true;

                return;

            }
    
    
    }

        #endregion
        private void button3_Click(object sender, EventArgs e)
        {
            pause_contiue();            
        }


        


        



        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Disk Wiper \n\rVersion 1.0 (Build 2011)\n\rBy: hsafavi321@gmail.com\r\nAll rights reserved.");
        }


        #region events


        private void pictureBox1_Click(object sender, EventArgs e)
        {
            
            //this is exactly the same as cancel button

            //////////////////////
            if (folderBrowserDialog1.SelectedPath == "")//chon tabe clean_files() path mikhad dakhele cancel(),pas call nakardim  cancel ra.
            {
                Application.Exit();
                return;
            }
            //////////////////////

            cancel();          
           
            Application.Exit();
            return;
           
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            //if (!pictureBox2.enabled) return;
            //MessageBox.Show("mgbhj");
        }





        private void button5_Click(object sender, EventArgs e)
        {
            

            //////////////////////
            if (folderBrowserDialog1.SelectedPath == "")//chon tabe clean_files() path mikhad dakhele cancel(),pas call nakardim  cancel ra.
            {
                Application.Exit();
                return;
            }
            //////////////////////

            cancel();

            
            Application.Exit();
            return;
        }

















        private void pictureBox3_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void pictureBox3_MouseEnter(object sender, EventArgs e)
        {
         pictureBox3.BackgroundImage = global::WindowsFormsApplication1.Properties.Resources.minimize_light;
        }

        private void pictureBox3_MouseLeave(object sender, EventArgs e)
        {
            pictureBox3.BackgroundImage = global::WindowsFormsApplication1.Properties.Resources.minimize;
        }

        

        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            pictureBox1.BackgroundImage = global::WindowsFormsApplication1.Properties.Resources.close;
        }

        private void pictureBox1_MouseEnter(object sender, EventArgs e)
        {
            pictureBox1.BackgroundImage = global::WindowsFormsApplication1.Properties.Resources.close_light;
        }

        private void pictureBox2_MouseDown(object sender, MouseEventArgs e)
        {
           p.X = e.X;
           p.Y = e.Y;

        }

         Point p = new Point();
        private void pictureBox2_MouseUp(object sender, MouseEventArgs e)
        {
            int deltax = e.X - p.X;
            int deltay = e.Y - p.Y;

            Point formxy = new Point();
            formxy.X += this.Location.X + deltax;
            formxy.Y += this.Location.Y + deltay;
            this.Location = formxy;
        }

#endregion
    }
}