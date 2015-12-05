using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BtmI2p.BasicAuthHttpJsonRpc.Client;
using BtmI2p.ExternalAppsLocalApi;
using BtmI2p.JsonRpcHelpers.Client;
using BtmI2p.MiscUtils;
using Nito.AsyncEx;
using NLog;
using Xunit;

namespace ExternalFullApiCallsExample
{
    public static class Program
    {
        private static async Task TestProxyApi()
        {
            // credentials and other details you can get from Main -> JSON RPC servers -> Settings, Proxy tab
            var httpClient = BasicAuthHttpJsonClientService<IProxyLocalJsonRpcApi>
                .CreateInstance(
                    new Uri(
                        $"http://localhost:14301"
                    ),
                    "BGZDje+o",
                    "91rnTGm2Z01P"
                ).Proxy;
            // check is proxy connected
            var proxyConnected = await httpClient.IsProxyServerConnected20151004().ConfigureAwait(false);
            Console.WriteLine($"Proxy connected: {proxyConnected}");
            if (!proxyConnected)
                return;
            // get proxy balance, refill if it's too low
            var proxyBalance = await httpClient.GetProxyServerBalance20151004().ConfigureAwait(false);
            Console.WriteLine($"Proxy balance {proxyBalance}");
            // get invoice data to refill balance by 35 BTM
            var invoiceData = await httpClient.GetInvoiceDataForReplenishment20151004(35).ConfigureAwait(false);
            Console.WriteLine(
                $"Invoice data: {invoiceData.TransferAmount}" +
                $" walletTo GUID {invoiceData.WalletTo}" +
                $" comment bytes b64 first 10 chars: {Convert.ToBase64String(invoiceData.CommentBytes.Take(10).ToArray())}"
            );
            // check is new version availavle on server (you should upgrade as fast as you get the notification, old client requests to wallet, message, mining, excahgne servers become been rejected immediately due to possible api changes)
            var newVersionAvailable = await httpClient.IsNewAppVersionAvailable20151004().ConfigureAwait(false);
            Console.WriteLine($"New version available: {newVersionAvailable}");
            // get client local time UTC
            var clientLocalTime = DateTime.ParseExact(
                await httpClient.GetNowLocalTime20151004().ConfigureAwait(false),
                WalletLocalJsonRpcApiArgsChecker20151004.DateTimeStringFormat20151004,
                CultureInfo.InvariantCulture
            );
            Console.WriteLine($"Local time: {clientLocalTime}");
            // get server time UTC, if it's too far from local, please update your client machine clock using NTP
            var serverTime = DateTime.ParseExact(
                await httpClient.GetNowServerTime20151004().ConfigureAwait(false),
                WalletLocalJsonRpcApiArgsChecker20151004.DateTimeStringFormat20151004,
                CultureInfo.InvariantCulture
            );
            Console.Write($"Server time: {serverTime}");
            // get (server - local) total seconds
            var serverClientTimeDiffSeconds =
                await httpClient.GetServerLocalTimeDiffSeconds20151004().ConfigureAwait(false);
            Console.WriteLine($"Server - local time diff seconds: {serverClientTimeDiffSeconds}");
        }

        private static async Task TestWalletApi()
        {
            // your wallet guid
            var baseWalletGuid = Guid.Parse("b0cc55b7-9b56-4ab9-94b7-2a9a907500fe");
            var secondWalletGuid = Guid.Parse("fe828d31-9e26-45e1-b6b1-29edcbcd6fb0");
            var nonExistentWalletGuid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var hmacCode = Convert.FromBase64String(
                "zUIlfcG+vlUxvSR+zPn9ODuAq8hkrq4ZijrMk8aUvk+5xRL8wGmhNi5buB17mV6QhpXvjlIj1cLexdW8Vqrl3A=="
            );
            Console.WriteLine($"Test wallet GUID: {baseWalletGuid}");
            // credentials and other details you can get from Main -> JSON RPC servers -> Settings, Wallet tab
            var httpClient = BasicAuthHttpJsonClientService<IWalletLocalJsonRpcApi>
                .CreateInstance(
                    new Uri(
                        $"http://localhost:14300"
                    ),
                    "PNXDMJm0",
                    "6A3M6nBlowBq"
                ).Proxy;
            // check is rpc calls allowed for the wallet
            var rpcAllowed = await httpClient.IsWalletRpcAllowed20151004(baseWalletGuid).ConfigureAwait(false);
            Console.WriteLine($"Rpc allowed: {rpcAllowed}");
            if (!rpcAllowed)
                return;
            // check is wallet connected in BitMoney client GUI
            var walletConnected = await httpClient.IsWalletConnected20151004(baseWalletGuid).ConfigureAwait(false);
            Console.WriteLine($"Wallet connected: {walletConnected}");
            if (!walletConnected)
                return;
            // get current wallet balance
            var walletBalance = await httpClient.GetBalance20151004(baseWalletGuid).ConfigureAwait(false);
            Console.WriteLine($"Wallet balance: {walletBalance}");
            // json exception handling example
            try
            {
                var nonExistentWalletBalance = await httpClient.GetBalance20151004(
                    nonExistentWalletGuid
                    ).ConfigureAwait(false);
            }
            catch (JsonRpcException e)
            {
                var errorCode = (EGeneralWalletLocalApiErrorCodes20151004)e.JsonErrorCode;
                Assert.Equal(errorCode,EGeneralWalletLocalApiErrorCodes20151004.WalletNotAllowed);
            }
            // check HMAC signature required
            var hmacSignatureRequired = await httpClient.HmacSignatureRpcRequired20151004().ConfigureAwait(false);
            Console.WriteLine($"HMAC signature required: {hmacSignatureRequired}");
            // define transfer parameters
            var requestGuid = Guid.NewGuid();
            const bool anonymousTransfer = false;
            long transferAmount = 10;
            var transferCommentUString = "Test comment #9999 测试";
            var transferCommentBytes = Encoding.UTF8.GetBytes(transferCommentUString);
            var transferCommentBytesLen = transferCommentBytes.Length;
            Console.WriteLine($"Transfer comment len: {transferCommentBytesLen}");
            // estimate fee
            var expectedFee = await httpClient.EstimateFee20151004(
                baseWalletGuid,
                secondWalletGuid,
                transferAmount,
                transferCommentBytesLen,
                anonymousTransfer
                ).ConfigureAwait(false);
            Console.WriteLine($"Expected fee: {expectedFee}");
            // check is wallet_to (second_wallet_guid) registered
            var walletToRegistered = false;
            while (true)
            {
                try
                {
                    walletToRegistered = await httpClient.IsWalletRegistered20151004(
                        baseWalletGuid,
                        secondWalletGuid
                        ).ConfigureAwait(false);
                    Console.WriteLine($"Wallet To registered: {walletToRegistered}");
                    break;
                }
                catch (JsonRpcException e)
                {
                    var errorCode = (EGeneralWalletLocalApiErrorCodes20151004) e.JsonErrorCode;
                    if (errorCode == EGeneralWalletLocalApiErrorCodes20151004.InQueueRetryLater)
                    {
                        await Task.Delay(1000).ConfigureAwait(false);
                        continue;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            if (!walletToRegistered)
            {
                Console.WriteLine("Second wallet is not registered");
                return;
            }
            var hmacSignature = new byte[0];
            var sentTimeString = DateTime.UtcNow.ToString(
                WalletLocalJsonRpcApiArgsChecker20151004.DateTimeStringFormat20151004,
                CultureInfo.InvariantCulture
            );
            // calc hmac signature
            if (hmacSignatureRequired)
                hmacSignature = WalletLocalJsonRpcApiArgsChecker20151004.GetSentTransfer20151004ArgsHmacAuthCode(
                    new WalletLocalJsonRpcApiArgsChecker20151004.SendTransfer20151004Args()
                    {
                        AnonymousTransfer = anonymousTransfer,
                        Amount = transferAmount,
                        CommentBytes = transferCommentBytes,
                        RequestGuid = requestGuid,
                        MaxFee = expectedFee,
                        SentTimeString = sentTimeString,
                        WalletFromGuid = baseWalletGuid,
                        WalletToGuid = secondWalletGuid
                    },
                    hmacCode
                );
            // sending
            var sameRequestGuid = await httpClient.SendTransfer20151004(
                requestGuid,
                baseWalletGuid,
                secondWalletGuid,
                transferAmount,
                sentTimeString,
                anonymousTransfer,
                transferCommentBytes,
                expectedFee,
                hmacSignature
            ).ConfigureAwait(false);
            Console.WriteLine($"Request GUID: {sameRequestGuid}");
            // get sent request status
            var recheckRequestStatus = false;
            ESentRequestStatus20151004 requestStatus;
            GetSentTransferRequestStatus20151004Response requestStatusResponse;
            while (true)
            {
                try
                {
                    requestStatusResponse = await httpClient.GetSentTransferRequestStatus20151004(
                        baseWalletGuid,
                        requestGuid,
                        recheckRequestStatus
                    ).ConfigureAwait(false);
                    requestStatus = requestStatusResponse.Status;
                    if (
                        requestStatus == ESentRequestStatus20151004.PreparedToSend
                        || requestStatus == ESentRequestStatus20151004.NotFound
                        )
                    {
                        await Task.Delay(1000).ConfigureAwait(false);
                        recheckRequestStatus = true;
                    }
                    else
                    {
                        break;
                    }
                }
                catch(JsonRpcException e)
                {
                    var errorCode = (EGeneralWalletLocalApiErrorCodes20151004) e.JsonErrorCode;
                    if (errorCode == EGeneralWalletLocalApiErrorCodes20151004.InQueueRetryLater)
                    {
                        await Task.Delay(1000).ConfigureAwait(false);
                        recheckRequestStatus = false;
                        continue;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            Console.WriteLine($"Request status {requestStatus}");
            List<Guid> relatedTransferGuidList = null;
            if (requestStatus == ESentRequestStatus20151004.SendFault)
            {
                var errCode = requestStatusResponse.ErrCode;
                if (errCode == ESentRequestFaultErrCodes20151004.ServerException)
                {
                    var generalServerError = requestStatusResponse.GeneralServerErrCode;
                    var serverError = requestStatusResponse.ServerErrCode;
                    var errMessage = requestStatusResponse.ErrMessage;
                    Console.WriteLine(
                        $"Something goes wrong server-side: general {generalServerError}" +
                        $", transfer {serverError}" +
                        $", message {errMessage}"
                        );
                }
                else if (errCode == ESentRequestFaultErrCodes20151004.UnknownError)
                {
                    Console.WriteLine(
                        $"Something goes wrong: {requestStatusResponse.ErrMessage}"
                        );
                }
                return;
            }
            else
            {
                relatedTransferGuidList = requestStatusResponse.RelatedTransferGuidList;
            }
            var transferGuid = relatedTransferGuidList.Single();
            Console.WriteLine($"Transfer GUID: {transferGuid}");
            // Get transfer info from base wallet
            TransferInfo20151004 sentTransferInfo;
            while (true)
            {
                try
                {
                    sentTransferInfo = await httpClient.GetSentTransferInfo20151004(
                        baseWalletGuid,
                        transferGuid
                    ).ConfigureAwait(false);
                    break;
                }
                catch (JsonRpcException e)
                {
                    var errorCode = (EGeneralWalletLocalApiErrorCodes20151004)e.JsonErrorCode;
                    if (errorCode == EGeneralWalletLocalApiErrorCodes20151004.InQueueRetryLater)
                    {
                        await Task.Delay(1000).ConfigureAwait(false);
                        continue;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            if (sentTransferInfo == null)
                return;
            Console.WriteLine($"Actual fee: {sentTransferInfo.Fee}");
            Console.WriteLine($"Whole transfer info data: {sentTransferInfo.WriteObjectToJson()}");
            // Get transfer info from second wallet
            TransferInfo20151004 receivedTransferInfo;
            while (true)
            {
                try
                {
                    receivedTransferInfo = await httpClient.GetReceivedTransferInfo20151004(
                        secondWalletGuid,
                        transferGuid
                        ).ConfigureAwait(false);
                    break;
                }
                catch (JsonRpcException e)
                {
                    var errorCode = (EGeneralWalletLocalApiErrorCodes20151004) e.JsonErrorCode;
                    if (errorCode == EGeneralWalletLocalApiErrorCodes20151004.InQueueRetryLater)
                    {
                        await Task.Delay(1000).ConfigureAwait(false);
                        continue;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            if (receivedTransferInfo == null)
                return;
            Console.WriteLine($"Whole received transfer info: {receivedTransferInfo.WriteObjectToJson()}");
            // Send some transfer back from second wallet
            requestGuid = Guid.NewGuid();
            sentTimeString = DateTime.UtcNow.ToString(
                WalletLocalJsonRpcApiArgsChecker20151004.DateTimeStringFormat20151004,
                CultureInfo.InvariantCulture
            );
            transferAmount = 8;
            // estimate fee
            expectedFee = await httpClient.EstimateFee20151004(
                secondWalletGuid,
                baseWalletGuid,
                transferAmount,
                transferCommentBytesLen,
                anonymousTransfer
            ).ConfigureAwait(false);
            Console.WriteLine($"2Expected fee: {expectedFee}");
            hmacSignature = new byte[0];
            // calc hmac signature
            if (hmacSignatureRequired)
            {
                hmacSignature = WalletLocalJsonRpcApiArgsChecker20151004.GetSentTransfer20151004ArgsHmacAuthCode(
                    new WalletLocalJsonRpcApiArgsChecker20151004.SendTransfer20151004Args()
                    {
                        AnonymousTransfer = anonymousTransfer,
                        Amount = transferAmount,
                        CommentBytes = transferCommentBytes,
                        RequestGuid = requestGuid,
                        MaxFee = expectedFee,
                        SentTimeString = sentTimeString,
                        WalletFromGuid = secondWalletGuid,
                        WalletToGuid = baseWalletGuid
                    },
                    hmacCode
                );
            }
            sameRequestGuid = await httpClient.SendTransfer20151004(
                requestGuid,
                secondWalletGuid,
                baseWalletGuid,
                transferAmount,
                sentTimeString,
                anonymousTransfer,
                transferCommentBytes,
                expectedFee,
                hmacSignature
            ).ConfigureAwait(false);
            Console.WriteLine($"2Request GUID: {sameRequestGuid}");
            // wait ultil request processed
            recheckRequestStatus = false;
            while (true)
            {
                try
                {
                    requestStatusResponse = await httpClient.GetSentTransferRequestStatus20151004(
                        secondWalletGuid,
                        requestGuid,
                        recheckRequestStatus
                        ).ConfigureAwait(false);
                    requestStatus = requestStatusResponse.Status;
                    if (requestStatus == ESentRequestStatus20151004.PreparedToSend
                        || requestStatus == ESentRequestStatus20151004.NotFound)
                    {
                        await Task.Delay(1000).ConfigureAwait(false);
                        recheckRequestStatus = true;
                    }
                    else
                    {
                        break;
                    }
                }
                catch (JsonRpcException e)
                {
                    var errorCode = (EGeneralWalletLocalApiErrorCodes20151004) e.JsonErrorCode;
                    if (errorCode == EGeneralWalletLocalApiErrorCodes20151004.InQueueRetryLater)
                    {
                        await Task.Delay(1000).ConfigureAwait(false);
                        recheckRequestStatus = false;
                        continue;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            Console.WriteLine($"2Request status: {requestStatus}");
            if (requestStatus != ESentRequestStatus20151004.Sent)
                return;
            transferGuid = requestStatusResponse.RelatedTransferGuidList.Single();
            Console.WriteLine($"2Transfer GUID: {transferGuid}");
            // Try to get all transfers in time range with data cursors
            var nowTimeUtc = DateTime.UtcNow;
            var startTime = nowTimeUtc.Subtract(TimeSpan.FromMinutes(10));
            var endTime = nowTimeUtc.AddMinutes(5);
            const bool fetchSentTransfers = true;
            const bool fetchReceivedTransfers = true;
            const bool stayOnline = false;
            var cursorNum = await httpClient.CreateTransferDataCursor20151004(
                baseWalletGuid,
                startTime.ToString(
                    WalletLocalJsonRpcApiArgsChecker20151004.DateTimeStringFormat20151004,
                    CultureInfo.InvariantCulture
                ),
                endTime.ToString(
                    WalletLocalJsonRpcApiArgsChecker20151004.DateTimeStringFormat20151004,
                    CultureInfo.InvariantCulture
                ),
                fetchSentTransfers,
                fetchReceivedTransfers,
                stayOnline
            ).ConfigureAwait(false);
            Console.WriteLine($"Cursor num: {cursorNum}");
            GetTransferDataCursorStatusResponse20151004 cursorStatusResponse;
            while (true)
            {
                cursorStatusResponse = await httpClient.GetTransferDataCursorStatus20151004(
                    cursorNum
                    ).ConfigureAwait(false);
                if(cursorStatusResponse.Status != ETransferDataCursorStatus20151004.Fetching)
                    break;
                await Task.Delay(1000).ConfigureAwait(false);
            }
            var totalTransfersInCursorCount = cursorStatusResponse.TotalTransferCount;
            Console.WriteLine($"Total transfers in cursor: {totalTransfersInCursorCount}");
            var offset = 0;
            var transferList = await httpClient.FetchTransferFromDataCursor20151004(
                cursorNum,
                offset,
                totalTransfersInCursorCount
            ).ConfigureAwait(false);
            Console.WriteLine($"Transfer list: {transferList.WriteObjectToJson()}");
            await httpClient.CloseTransferDataCursor20151004(cursorNum).ConfigureAwait(false);
        }

        private static async Task MainAsync(string[] args)
        {
            //await TestProxyApi().ConfigureAwait(false);
            await TestWalletApi().ConfigureAwait(false);
        }

        private static readonly Logger _log
            = LogManager.GetCurrentClassLogger();
        public static void Main(string[] args)
        {
            AsyncContext.Run(() => MainAsync(args));
        }
    }
}
