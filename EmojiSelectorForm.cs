using System;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;


namespace betochat
{
    public partial class EmojiSelectorForm : Form
    {
        public event EventHandler<string> EmojiSelected;

        public EmojiSelectorForm()
        {
            InitializeComponent();

        }

        private void Emoji_Click(object sender, EventArgs e)
        {
            Button emojiButton = (Button)sender;
            string emoji = emojiButton.Text;
            EmojiSelected?.Invoke(this, emoji);
            this.Close();
        }



        private void EmojiSelectorForm_Load(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            string emoji = "🙂"; 

        
            Sohbet_Ekrani mainForm = Application.OpenForms.OfType<Sohbet_Ekrani>().FirstOrDefault();

            if (mainForm != null)
            {
            
                mainForm.MessageTextBox.Text += emoji;
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            string emoji = "😎";


            Sohbet_Ekrani mainForm = Application.OpenForms.OfType<Sohbet_Ekrani>().FirstOrDefault();

            if (mainForm != null)
            {

                mainForm.MessageTextBox.Text += emoji;
            } 
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            string emoji = "😭";


            Sohbet_Ekrani mainForm = Application.OpenForms.OfType<Sohbet_Ekrani>().FirstOrDefault();

            if (mainForm != null)
            {

                mainForm.MessageTextBox.Text += emoji;
            } 
        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {
            string emoji = "😉";


            Sohbet_Ekrani mainForm = Application.OpenForms.OfType<Sohbet_Ekrani>().FirstOrDefault();

            if (mainForm != null)
            {

                mainForm.MessageTextBox.Text += emoji;
            }
        }

        private void pictureBox6_Click(object sender, EventArgs e)
        {
            string emoji = "😛";


            Sohbet_Ekrani mainForm = Application.OpenForms.OfType<Sohbet_Ekrani>().FirstOrDefault();

            if (mainForm != null)
            {

                mainForm.MessageTextBox.Text += emoji;
            }
        }
    }
}
