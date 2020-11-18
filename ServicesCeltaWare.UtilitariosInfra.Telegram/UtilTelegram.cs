using System;
using System.Collections.Generic;
using System.Text;

namespace ServicesCeltaWare.UtilitariosInfra
{
    public class UtilTelegram
    {
        private string token;
        public UtilTelegram(string _token)
        {
            this.token = _token;
        }

        public void SendMessage(string message, string destinationId)
        {
            try
            {
                Send(message, destinationId);
            }
            catch (Exception err)
            {
                throw err;
            }
        }

        private void Send(string text, string destID)
        {
            try
            {
                var bot = new Telegram.Bot.TelegramBotClient(token);
                bot.SendTextMessageAsync(destID, text);
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
