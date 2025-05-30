using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SQLServerExporter.Forms
{
    public partial class Form_ConnectionSettings : Form, INotifyPropertyChanged
    {
        private string _serverAddress;
        private string _databaseName;
        private string _userName;
        private string _password;
        private bool _trustedCertificate = false;

        public string ServerAddress 
        {
            get => _serverAddress;
            set
            {
                if (_serverAddress != value)
                {
                    _serverAddress = value;
                    OnPropertyChanged();
                }
            }
        }
        public string DatabaseName
        {
            get => _databaseName;
            set
            {
                if (_databaseName != value)
                {
                    _databaseName = value;
                    OnPropertyChanged();
                }
            }
        }
        public string UserName  
        {
            get => _userName;
            set
            {
                if (_userName != value)
                {
                    _userName = value;
                    OnPropertyChanged();
                }
            }
        }
        public string Password
        {
            get => _password;
            set
            {
                if (_password != value)
                {
                    _password = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool TrustedCertificate
        {
            get => _trustedCertificate;
            set
            {
                if (_trustedCertificate != value)
                {
                    _trustedCertificate = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Form_ConnectionSettings()
        {
            InitializeComponent();
            this.DialogResult = DialogResult.Cancel;
            SetDataBindings();
        }

        private void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SetDataBindings()
        {
            serverTB.DataBindings.Add("Text", this, nameof(ServerAddress));
            databaseTB.DataBindings.Add("Text", this, nameof(DatabaseName));
            usernameTB.DataBindings.Add("Text", this, nameof(UserName));
            passwordTB.DataBindings.Add("Text", this, nameof(Password));
            trustedCB.DataBindings.Add("Checked", this, nameof(TrustedCertificate));
        }

        private void ConfirmButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
