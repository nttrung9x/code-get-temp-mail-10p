﻿using System.Threading;
using TempMail.API;

namespace TempMail.Sample
{
    public class TempMailSample
    {
        public static void Sample()
        {
            var client = new Client();

            // To get a new temporary email
            client.StartNewSession();

            // To get the available domains
            var availableDomains = client.AvailableDomains;

            // Sends fake mails from your email to generated temporary email
            // Note: edit sender email credentials (email, password)
            // Note: you can put a break point and use any free email sender service online instead
            Common.SendFakeMails(2, client.Email, 1000);

            // Wait for the mails to reach the tempmail
            Thread.Sleep(10000);

            // To get Mailbox
            var mails = client.Inbox.Refresh();

            // Prints Client Session data like current email, mails, ...etc
            // Note: edit to print what you need
            Common.PrintClientData(client);

            // To change email to a specific login@domain
            client.Change("loginexample", availableDomains[0]);

            // To delete email and get a new one
            client.Delete();

            // To get the current email
            var email = client.Email;
        }
    }
}