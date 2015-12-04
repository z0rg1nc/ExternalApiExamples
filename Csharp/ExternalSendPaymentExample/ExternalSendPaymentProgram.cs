using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BtmI2p.BasicAuthHttpJsonRpc.Client;
using BtmI2p.ExternalAppsLocalApi;
using CommandLine;
using Nito.AsyncEx;
using NLog;
using NLog.LayoutRenderers;

namespace ExternalSendPaymentExample
{
    public class ExternalSendPaymentOptions
    {
        [Option("host", Required = true)]
        public string ServerHostName { get; set; }

        [Option("host-port", Required = true)]
        public int ServerPort { get; set; }

        [Option("username", Required = true)]
        public string Username { get; set; }

        [Option("password", Required = true)]
        public string Password { get; set; }
        /**/
        [Option("wallet-from", Required = true)]
        public string WalletFrom { get; set; }

        [Option("wallet-to", Required = true)]
        public string WalletTo { get; set; }

        [Option("amount", Required = true)]
        public long Amount { get; set; }

        [Option("anonymous", Required = false, DefaultValue = 0)]
        public int AnonymousTransfer { get; set; }

        [Option("comment-bytes-b64", Required = false, DefaultValue = "")]
        public string CommentBytesBase64 { get; set; }

        [Option(
            "request-guid", 
            Required = false, 
            DefaultValue = "00000000-0000-0000-0000-000000000000"
        )]
        public string RequestGuid { get; set; }

        [Option("hmac-key-b64", Required = true)]
        public string HmacKeyCodeBase64 { get; set; }
    }

    public class ExternalSendPaymentProgram
    {
        private static async Task MainAsync(string[] args)
        {
            try
            {
                CultureInfo.DefaultThreadCurrentUICulture
                    = CultureInfo.InvariantCulture;
            }
            catch (NotImplementedException)
            {
            }
            /**/
            try
            {
                var options = new ExternalSendPaymentOptions();
                if (Parser.Default.ParseArguments(args, options))
                {
                    var walletFromGuid = Guid.Parse(options.WalletFrom);
                    var walletToGuid = Guid.Parse(options.WalletTo);
                    var anonymous = options.AnonymousTransfer != 0;
                    var requestGuid = Guid.Parse(options.RequestGuid);
                    if (requestGuid == Guid.Empty)
                        requestGuid = Guid.NewGuid();
                    byte[] commentBytes = Convert.FromBase64String(
                        options.CommentBytesBase64
                    );
                    byte[] hmacKey = Convert.FromBase64String(
                        options.HmacKeyCodeBase64
                    );
                    var transport
                        = BasicAuthHttpJsonClientService<IWalletLocalJsonRpcApi>
                            .CreateInstance(
                                new Uri(
                                    $"http://{options.ServerHostName}:{options.ServerPort}/"
                                ),
                                options.Username,
                                options.Password
                            );
                    var proxy = transport.Proxy;
                    var isWalletConnected = await proxy.IsWalletConnected20151004(
                        walletFromGuid
                    ).ConfigureAwait(false);
                    _log.Trace("Wallet is {0}",isWalletConnected ? "connected" : "cisconnected");
                    if(!isWalletConnected)
                        throw new Exception("Wallet is not connected");
                    var curBalance = await proxy.GetBalance20151004(
                        walletFromGuid
                    ).ConfigureAwait(false);
                    _log.Trace("Current balance {0}", curBalance);
                    if(curBalance < options.Amount)
                        throw new Exception("Not enough funds");
                    string dtString = DateTime.UtcNow.ToString(
                        WalletLocalJsonRpcApiArgsChecker20151004.DateTimeStringFormat20151004,
                        CultureInfo.InvariantCulture
                    );
                    var sendTransferArgs = new WalletLocalJsonRpcApiArgsChecker20151004.SendTransfer20151004Args()
                    {
                        Amount = options.Amount,
                        AnonymousTransfer = options.AnonymousTransfer > 0,
                        RequestGuid = requestGuid,
                        CommentBytes = commentBytes,
                        MaxFee = 0,
                        SentTimeString = dtString,
                        WalletFromGuid = walletFromGuid,
                        WalletToGuid = walletToGuid
                    };
                    requestGuid = await proxy.SendTransfer20151004(
                        sendTransferArgs.RequestGuid,
                        sendTransferArgs.WalletFromGuid,
                        sendTransferArgs.WalletToGuid,
                        sendTransferArgs.Amount,
                        sendTransferArgs.SentTimeString,
                        sendTransferArgs.AnonymousTransfer,
                        sendTransferArgs.CommentBytes,
                        sendTransferArgs.MaxFee,
                        WalletLocalJsonRpcApiArgsChecker20151004.GetSentTransfer20151004ArgsHmacAuthCode(
                            sendTransferArgs,
                            hmacKey
                        )
                    ).ConfigureAwait(false);
                    _log.Trace("Request GUID {0}", requestGuid);
                }
                else
                {
                    throw new Exception(
                        "Parse command line args error"
                    );
                }
            }
            catch (Exception exc)
            {
                _log.Error(
                    "Unexpected error '{0}'",
                    exc.ToString()
                );
            }
        }
        private static readonly Logger _log
            = LogManager.GetCurrentClassLogger();
        public static void Main(string[] args)
        {
            AsyncContext.Run(() => MainAsync(args));
        }
    }
}
