using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
using static System.IO.Ports.SerialPort;
using System.IO;



namespace WindowsFormsApplication7
{
    public partial class Form1 : Form
    {
        private bool dirtyFlag = false;        //ダーティーフラグ 
        private bool readOnlyFlag = false;  //読み取り専用フラグ
        private string editFilePath = "";      //編集中のファイルのパス
        private class BuadRateItem : Object
        {
            private string m_name = "";
            private int m_value = 0;

            // 表示名称
            public string NAME
            {
                set { m_name = value; }
                get { return m_name; }
            }

            // ボーレート設定値.
            public int BAUDRATE
            {
                set { m_value = value; }
                get { return m_value; }
            }

            // コンボボックス表示用の文字列取得関数.
            public override string ToString()
            {
                return m_name;
            }
        }
        private class HandShakeItem : Object
        {
            private string m_name = "";
            private Handshake m_value = Handshake.None;

            // 表示名称
            public string NAME
            {
                set { m_name = value; }
                get { return m_name; }
            }

            // 制御プロトコル設定値.
            public Handshake HANDSHAKE
            {
                set { m_value = value; }
                get { return m_value; }
            }

            // コンボボックス表示用の文字列取得関数.
            public override string ToString()
            {
                return m_name;
            }
        }

        private delegate void Delegate_RcvDataToTextBox(string data);
        public Form1()
        {
            InitializeComponent();
            textBox4.Text = "7";

            //! 利用可能なシリアルポート名の配列を取得する.
            string[] PortList = SerialPort.GetPortNames();

            comboBox1.Items.Clear();
            button1.Text = "接続";
            button2.Text = "送信";
            button3.Text = "コード書き込み";

            //! シリアルポート名をコンボボックスにセットする.
            foreach (string PortName in PortList)
            {
                comboBox1.Items.Add(PortName);
            }
            if (comboBox1.Items.Count > 0)
            {
                comboBox1.SelectedIndex = 0;
            }

            comboBox2.Items.Clear();

            // ボーレート選択コンボボックスに選択項目をセットする.
            BuadRateItem baud;
            baud = new BuadRateItem();
            baud.NAME = "4800bps";
            baud.BAUDRATE = 4800;
            comboBox2.Items.Add(baud);

            baud = new BuadRateItem();
            baud.NAME = "9600bps";
            baud.BAUDRATE = 9600;
            comboBox2.Items.Add(baud);

            baud = new BuadRateItem();
            baud.NAME = "19200bps";
            baud.BAUDRATE = 19200;
            comboBox2.Items.Add(baud);

            baud = new BuadRateItem();
            baud.NAME = "115200bps";
            baud.BAUDRATE = 115200;
            comboBox2.Items.Add(baud);
            comboBox2.SelectedIndex = 1;

            comboBox3.Items.Clear();

            // フロー制御選択コンボボックスに選択項目をセットする.
            HandShakeItem ctrl;
            ctrl = new HandShakeItem();
            ctrl.NAME = "なし";
            ctrl.HANDSHAKE = Handshake.None;
            comboBox3.Items.Add(ctrl);

            ctrl = new HandShakeItem();
            ctrl.NAME = "XON/XOFF制御";
            ctrl.HANDSHAKE = Handshake.XOnXOff;
            comboBox3.Items.Add(ctrl);

            ctrl = new HandShakeItem();
            ctrl.NAME = "RTS/CTS制御";
            ctrl.HANDSHAKE = Handshake.RequestToSend;
            comboBox3.Items.Add(ctrl);

            ctrl = new HandShakeItem();
            ctrl.NAME = "XON/XOFF + RTS/CTS制御";
            ctrl.HANDSHAKE = Handshake.RequestToSendXOnXOff;
            comboBox3.Items.Add(ctrl);
            comboBox3.SelectedIndex = 0;

            // 送受信用のテキストボックスをクリアする.
            textBox1.Clear();
            textBox2.Clear();
            textBox3.Clear();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen == true)
            {
                //! シリアルポートをクローズする.
                serialPort1.Close();

                //! ボタンの表示を[切断]から[接続]に変える.
                button1.Text = "接続";
            }
            else
            {
                //! オープンするシリアルポートをコンボボックスから取り出す.
                serialPort1.PortName = comboBox1.SelectedItem.ToString();

                //! ボーレートをコンボボックスから取り出す.
                BuadRateItem baud = (BuadRateItem)comboBox2.SelectedItem;
                serialPort1.BaudRate = baud.BAUDRATE;

                //! データビットをセットする.
                serialPort1.DataBits = int.Parse(textBox4.Text);

                //! パリティビットをセットする. (パリティビット = なし)
                serialPort1.Parity = Parity.None;

                //! ストップビットをセットする. (ストップビット = 1ビット)
                serialPort1.StopBits = StopBits.One;

                //! フロー制御をコンボボックスから取り出す.
                HandShakeItem ctrl = (HandShakeItem)comboBox3.SelectedItem;
                serialPort1.Handshake = ctrl.HANDSHAKE;

                //! 文字コードをセットする.
                serialPort1.Encoding = Encoding.ASCII;

                try
                {
                    //! シリアルポートをオープンする.
                    serialPort1.Open();

                    //! ボタンの表示を[接続]から[切断]に変える.
                    button1.Text = "切断";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //! シリアルポートをオープンしていない場合、処理を行わない.
            if (serialPort1.IsOpen == false)
            {
                return;
            }

            try
            {
                //! 受信データを読み込む.
                string data = serialPort1.ReadExisting();

                //! 受信したデータをテキストボックスに書き込む.
                Invoke(new Delegate_RcvDataToTextBox(RcvDataToTextBox), new Object[] { data });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
        private void RcvDataToTextBox(string data)
        {
            //! 受信データをテキストボックスの最後に追記する.
            if (data != null)
            {
                textBox3.AppendText(data);
            }
        }

        private void 接続(object sender, PaintEventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            //! シリアルポートをオープンしていない場合、処理を行わない.
            if (serialPort1.IsOpen == false)
            {
                return;
            }
            //! テキストボックスから、送信するテキストを取り出す.
            String data = textBox1.Text + "\r\n";

            //! 送信するテキストがない場合、データ送信は行わない.
            if (string.IsNullOrEmpty(data) == true)
            {
                return;
            }

            try
            {
                //! シリアルポートからテキストを送信する.
                serialPort1.Write(data);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //! シリアルポートをオープンしていない場合、処理を行わない.
            if (serialPort1.IsOpen == false)
            {
                return;
            }
            //! テキストボックスから、送信するテキストを取り出す.
            String data = textBox2.Text + "\r\n";

            //! 送信するテキストがない場合、データ送信は行わない.
            if (string.IsNullOrEmpty(data) == true)
            {
                return;
            }

            try
            {
                //! シリアルポートからテキストを送信する.
                serialPort1.Write(data);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void setDirty(bool flag)
        {
            dirtyFlag = flag;
            //読み取り専用でファイルがオープンされている場合、
            //[上書き(&S)] メニューアイテムは常に無効
            上書き保存ToolStripMenuItem.Enabled = (readOnlyFlag) ? false : flag;
        }
        private bool confirmDestructionText(string msgboxTitle)
        {
            const string MSG_BOX_STRING = "ファイルは保存されていません。\n\n編集中のテキストは破棄されます。\n\nよろしいですか?";
            if (!dirtyFlag) return true;
            return (MessageBox.Show(MSG_BOX_STRING, msgboxTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes);
        }

        private void 新規作成ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            const string MSG_BOX_TITLE = "ファイルの新規作成";     //編集中のテキストがないか確認
            if (confirmDestructionText(MSG_BOX_TITLE))
            {
                this.Text = "新規ファイル"; //アプリケーションのタイトルを変更
                textBox1.Clear();      //テキストボックスの内容をクリア
                editFilePath = "";           //編集中ファイルのパスをクリア
                setDirty(false);               //ダーティーフラグと[上書き..]メニューを設定
            }
        }

        private void 開くToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog(this);
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            const string TITLE_EXTN_ReadOnly = " (読み取り専用)";
            const string MSGBOX_TITLE = "ファイル オープン";

            //選択されたファイルのパスを取得
            editFilePath = openFileDialog1.FileName;

            //ファイルダイアログで読み取り専用が選択されたかどうかの値を取得
            readOnlyFlag = openFileDialog1.ReadOnlyChecked;

            //読み取り専用で開いた場合にタイトル(ファイル名)に "読み取り専用" の文字をつける
            this.Text = (readOnlyFlag)
                 ? openFileDialog1.SafeFileName + TITLE_EXTN_ReadOnly : openFileDialog1.SafeFileName;

            //ダーティーフラグのリセット
            setDirty(false);

            try
            {
                //テキストファイルの内容をテキストボックスにロード
                textBox2.Text = File.ReadAllText(editFilePath, Encoding.Default);
            }
            catch (Exception ex)
            {
                //ファイルの読み込みでエラーが発生した場合に Exception の内容を表示
                MessageBox.Show(this, ex.Message, MSGBOX_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ShowSaveDateTime()
        {
            const string STATUS_STRING = "に保存しました";

            //ステータスバー上のラベルに表示
            toolStripStatusLabel1.Text = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + STATUS_STRING;
        }

        private void 上書き保存ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            const string MSGBOX_TITLE = "ファイルの上書き保存";

            //保存先のファイルが存在するかチェック
            if (File.Exists(editFilePath))
            {
                try
                {
                    //テキストボックスの内容をファイルに書き込み
                    File.WriteAllText(editFilePath, textBox2.Text, Encoding.Default);

                    //ダーティーフラグをリセット
                    setDirty(false);

                    //ステータスバーに保存日時を表示
                    ShowSaveDateTime();
                }
                catch (Exception ex)
                {
                    //ファイルの書き込みでエラーが発生した場合に Exception の内容を表示
                    MessageBox.Show(this, ex.Message, MSGBOX_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                string MSG_BOX_STRING = "ファイル\"" + editFilePath
                     + "\" のパスは正しくありません。\n\nディレクトリが存在するか確認してください。";
                MessageBox.Show(MSG_BOX_STRING, MSGBOX_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            setDirty(true);
        }

        private void 名前を付けて保存ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //ファイルが新規作成だった場合の名前
            const string NEW_FILE_NAME = "新規テキストファイル.txt";
            //編集中のファイルのフルパスからファイル名だけを取得
            string fileNameString = GetFileNameString(editFilePath, '\\');

            //ファイル名が空白であった場合は　"新規テキストファイル.txt" をセット
            saveFileDialog1.FileName = (fileNameString == "")
                 ? NEW_FILE_NAME : fileNameString;
            saveFileDialog1.ShowDialog(this);
        }
        private string GetFileNameString(string filePath, char separateChar)
        {
            try
            {
                string[] strArray = filePath.Split(separateChar);
                return strArray[strArray.Length - 1];
            }
            catch
            { return ""; }
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            const string MSGBOX_TITLE = "名前を付けて保存";

            //ファイルダイアログで指定された保存先パスを取得
            editFilePath = saveFileDialog1.FileName;
            try
            {
                //ファイルの書き込み
                File.WriteAllText(editFilePath, textBox1.Text, Encoding.Default);

                //ファイル名をウィンドウのタイトルに設定
                this.Text = GetFileNameString(editFilePath, '\\');

                //ダーティーフラグのリセット
                setDirty(false);

                //ステータスバーに保存日時を表示
                ShowSaveDateTime();
            }
            catch (Exception ex)
            {
                //エラー発生の際に Exception の内容を表示
                MessageBox.Show(this, ex.Message, MSGBOX_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
            private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            const string MSGBOX_TITLE = "アプリケーションの終了";

            if (confirmDestructionText(MSGBOX_TITLE))
            {
                // Form1の破棄
                this.Dispose();
            }
            else
            {
                // ウィンドウを閉じるのをやめる
                e.Cancel = true;
            }
        }
        private void menuEnd_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }
    }
}

