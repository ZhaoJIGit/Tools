using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TaskMGPro.Common
{
    public class Message
    {
        public static bool Show(string msg)
        {
            MessageBox.Show(msg);
            return true;
        }
        public static bool Question(string msg)
        {
            MessageBoxResult result = MessageBox.Show(
                msg,
                "询问",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
