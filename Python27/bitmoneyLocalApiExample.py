#!/usr/bin/env python
# coding: utf-8

import pyjsonrpc
import base64
import urllib2
import datetime
import struct
import hmac
import hashlib
import uuid
import time
from enum import Enum

# pip install python-jsonrpc
# pip install enum34

# every datetime string has 'yyyyMMddHHmmss' (.NET) or '%Y%m%d%H%M%S' (Python) format UTC
# urllib2.URLError exception - connection refused
# pyjsonrpc.rpcerror.JsonRpcError

class EGeneralProxyLocalApiErrorCodes20151004(Enum):
	NoErrors = 100
	ProxyNotConnected = 101
	WrongArgs = 102
	RequestLifetimeExpired = 103
	UnknownError = 104

def test_proxy_api():
	# credentials and other details you can get from Main -> JSON RPC servers -> Settings, Proxy tab
	http_client = pyjsonrpc.HttpClient(
		url = "http://localhost:14301",
		username = "BGZDje+o",
		password = "91rnTGm2Z01P"
	)
	# check is proxy connected
	proxy_connected = http_client.IsProxyServerConnected20151004()
	print "Proxy connected:", proxy_connected
	if proxy_connected == False:
		return
	# get proxy balance, refill if it's too low
	proxy_balance = http_client.GetProxyServerBalance20151004()
	print "Proxy balance:", proxy_balance
	# get invoice data to refill balance by 35 BTM
	invoice_data = http_client.GetInvoiceDataForReplenishment20151004(35)
	print "Invoice data: amount", invoice_data["TransferAmount"], "walletTo GUID", invoice_data["WalletTo"], "comment bytes b64 first 10 chars:", invoice_data["CommentBytes"][:10]
	# check is new version availavle on server (you should upgrade as fast as you get the notification, old client requests to wallet, message, mining, excahgne servers become been rejected immediately due to possible api changes)
	new_version_available = http_client.IsNewAppVersionAvailable20151004()
	print "New version available:", new_version_available
	# get client local time UTC
	client_local_time = http_client.GetNowLocalTime20151004()
	print "Local time:", client_local_time
	# get server time UTC, if it's too far from local, please update your client machine clock using NTP
	server_time = http_client.GetNowServerTime20151004()
	print "Server time:", server_time
	# get (server - local) total seconds
	server_client_time_diff_seconds = http_client.GetServerLocalTimeDiffSeconds20151004()
	print "Server - local time diff seconds:", server_client_time_diff_seconds
	
class EGeneralWalletLocalApiErrorCodes20151004(Enum):
	NoErrors = 100
	WalletNotAllowed = 101
	WalletNotConnected = 102
	WrongArgs = 103
	RequestLifetimeExpired = 104
	ObsoleteMethod = 105
	InQueueRetryLater = 106 # often used, instead of long polling
	RpcServerDisabled = 107
	CursorNotFound = 108
	WrongAuthCode = 109
	UnknownError = 110
	
class EProcessSimpleTransferErrCodes20151004(Enum):
	NoErrors = 0
	WalletToNotExist = 1
	NotEnoughFunds = 2
	CommentKeyIsNotRegistered = 3
	WrongCommentKey = 4
	ExpiredCommentKey = 5
	CommentSizeTooBig = 6
	WrongTransferAmount = 7
	TransferAmountLessThanWalletToRequires = 8
	AnonymousKeyWithNotAnonymousTransfer = 9
	NotAnonymousKeyWithAnonymousTransfer = 10
	TransferGuidAlreadyRegistered = 11
	ExpectedFeeMoreThanRequestMaxFee = 12
	
class EWalletGeneralErrCodes20151004(Enum):
	NoErrors = 100
	WrongRequest = 101
	WalletGuidForbidden = 102
	OtherWalletGuidForbidden = 103
	WrongServerWalletGuidRange = 104
	OtherWalletGuidNotExist = 105
	AlreadyRegisteredByOtherWallet = 106
	WalletNotRegistered = 107
	OtherWalletNotRegistered = 108
	
class ESentRequestStatus20151004(Enum):
	NotFound = 0
	PreparedToSend = 1
	SendFault = 2
	Sent = 3
	
class ESentRequestFaultErrCodes20151004(Enum):
	NoErrors = 0
	WalletToNotExist = 1
	ServerException = 2
	UnknownError = 3
	
class ETransferDataCursorStatus20151004(Enum):
	Fetching = 0
	Complete = 1
	WalletDisconnected = 2
	UnknownError = 3
	
def ticks_since_epoch(start_time_override):
    start_time = start_time_override
    ticks_per_ms = 10000
    ms_per_second = 1000
    ticks_per_second = ticks_per_ms * ms_per_second
    span = start_time - datetime.datetime(1, 1, 1)
    ticks = int(span.total_seconds() * ticks_per_second)
    return ticks
	
def str_to_hex(s):
	return ':'.join(x.encode('hex') for x in s)
	
def calc_hmac_code(request_guid, sent_time_string, wallet_from_guid, wallet_to_guid, amount, anonymous_transfer, comment_bytes_b64, max_fee, hmac_code_b64):
	request_guid_bytes = uuid.UUID(request_guid).bytes_le
	#print "Request GUID hex repr:", str_to_hex(request_guid_bytes)
	sent_time = datetime.datetime.strptime(sent_time_string,'%Y%m%d%H%M%S')
	sent_time_ticks = ticks_since_epoch(sent_time)
	wallet_from_guid_bytes = uuid.UUID(wallet_from_guid).bytes_le
	wallet_to_guid_bytes = uuid.UUID(wallet_to_guid).bytes_le
	packed_data = request_guid_bytes + struct.pack('<q', sent_time_ticks) + wallet_from_guid_bytes + wallet_to_guid_bytes + struct.pack('<q?', amount, anonymous_transfer) + base64.b64decode(comment_bytes_b64) + struct.pack('<q', max_fee)
	#print "Packed data:", packed_data
	digest = hmac.new(base64.b64decode(hmac_code_b64),packed_data,digestmod=hashlib.sha256).digest()
	#print "HMAC code hex repr:", str_to_hex(digest)
	return base64.b64encode(digest)
		
def test_wallet_api():
	# your wallet guid
	base_wallet_guid = "b0cc55b7-9b56-4ab9-94b7-2a9a907500fe"
	second_wallet_guid = "fe828d31-9e26-45e1-b6b1-29edcbcd6fb0"
	non_existent_wallet_guid = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
	hmac_code_b64 = "zUIlfcG+vlUxvSR+zPn9ODuAq8hkrq4ZijrMk8aUvk+5xRL8wGmhNi5buB17mV6QhpXvjlIj1cLexdW8Vqrl3A=="
	##########
	print "Test wallet GUID:", base_wallet_guid
	# credentials and other details you can get from Main -> JSON RPC servers -> Settings, Wallet tab
	http_client = pyjsonrpc.HttpClient(
		url = "http://localhost:14300",
		username = "PNXDMJm0",
		password = "6A3M6nBlowBq"
	)
	# check is rpc calls allowed for the wallet
	rpc_allowed = http_client.IsWalletRpcAllowed20151004(base_wallet_guid)
	print "Rpc allowed:", rpc_allowed
	if rpc_allowed == False:
		return
	# check is wallet connected in BitMoney client GUI
	wallet_connected = http_client.IsWalletConnected20151004(base_wallet_guid)
	print "Wallet connected:", wallet_connected
	if wallet_connected == False:
		return
	# get current wallet balance
	wallet_balance = http_client.GetBalance20151004(base_wallet_guid)
	print "Wallet balance:", wallet_balance
	# json exception handling example
	try:
		non_exist_wallet_balance = http_client.GetBalance20151004(non_existent_wallet_guid)
	except pyjsonrpc.rpcerror.JsonRpcError as e:
		error_code = int(e.code)
		print "Non existent wallet API call error code:", EGeneralWalletLocalApiErrorCodes20151004(error_code) # WalletNotAllowed
	# check HMAC signature required
	hmac_signature_required = http_client.HmacSignatureRpcRequired20151004()
	print "HMAC signature required:", hmac_signature_required
	# define transfer parameters
	request_guid = str(uuid.uuid4())
	anonymous_transfer = False
	transfer_amount = 10
	transfer_comment_u_string = u"Test comment #9999 测试"
	transfer_comment_bytes = transfer_comment_u_string.encode('utf-8')
	transfer_comment_bytes_len = len(transfer_comment_bytes)
	transfer_comment_bytes_b64 = base64.b64encode(transfer_comment_bytes)
	print "Transfer comment len:", transfer_comment_bytes_len, "b64:", transfer_comment_bytes_b64
	# estimate fee
	expected_fee = http_client.EstimateFee20151004(base_wallet_guid, second_wallet_guid, transfer_amount, transfer_comment_bytes_len, anonymous_transfer)
	print "Expected fee:", expected_fee
	# check is wallet_to (second_wallet_guid) registered
	wallet_to_registered = False
	while True:
		try:
			wallet_to_registered = http_client.IsWalletRegistered20151004(base_wallet_guid, second_wallet_guid)
			print "Wallet To registered:", wallet_to_registered
			break
		except pyjsonrpc.rpcerror.JsonRpcError as e:
			error_code = EGeneralWalletLocalApiErrorCodes20151004(e.code)
			if error_code == EGeneralWalletLocalApiErrorCodes20151004.InQueueRetryLater:
				time.sleep(1) # wait ultil data loading
				continue
			else:
				raise
	if wallet_to_registered == False:
		print "Second wallet is not registered"
		return
	# calc hmac signature
	hmac_signature_b64 = ""
	sent_time_string = datetime.datetime.utcnow().strftime('%Y%m%d%H%M%S')
	if hmac_signature_required == True:
		hmac_signature_b64 = calc_hmac_code(request_guid, sent_time_string, base_wallet_guid, second_wallet_guid, transfer_amount, anonymous_transfer, transfer_comment_bytes_b64, expected_fee, hmac_code_b64)
	# sending
	same_request_guid = http_client.SendTransfer20151004(request_guid, base_wallet_guid, second_wallet_guid, transfer_amount, sent_time_string, anonymous_transfer, transfer_comment_bytes_b64, expected_fee, hmac_signature_b64)
	print "Request GUID:", same_request_guid
	# get sent request status
	request_status_response = 0
	request_status = 0
	recheck_request_status = False
	while True:
		try:
			request_status_response = http_client.GetSentTransferRequestStatus20151004(base_wallet_guid, request_guid, recheck_request_status)
			request_status = ESentRequestStatus20151004(request_status_response["Status"])
			if (request_status == ESentRequestStatus20151004.PreparedToSend) or (request_status == ESentRequestStatus20151004.NotFound):
				time.sleep(1)
				recheck_request_status = True
			else:
				break
		except pyjsonrpc.rpcerror.JsonRpcError as e:
			error_code = EGeneralWalletLocalApiErrorCodes20151004(e.code)
			if error_code == EGeneralWalletLocalApiErrorCodes20151004.InQueueRetryLater:
				time.sleep(1) # wait ultil data loading
				recheck_request_status = False
				continue
			else:
				raise
	print "Request status:", request_status
	related_transfer_guid_list = 0
	if request_status == ESentRequestStatus20151004.SendFault:
		err_code = ESentRequestFaultErrCodes20151004(request_status_response["ErrCode"])
		if err_code == ESentRequestFaultErrCodes20151004.ServerException:
			general_server_error = EWalletGeneralErrCodes20151004(request_status_response["GeneralServerErrCode"])
			server_error = EProcessSimpleTransferErrCodes20151004(request_status_response["ServerErrCode"])
			err_message = request_status_response["ErrMessage"]
			print "Something goes wrong server-side: general", general_server_error, ", transfer", server_error, ", message", err_message
		elif err_code == ESentRequestFaultErrCodes20151004.UnknownError:
			err_message = request_status_response["ErrMessage"]
			print "Something goes wrong:", err_message
		return
	else:
		related_transfer_guid_list = request_status_response["RelatedTransferGuidList"]
	transfer_guid = str(uuid.UUID(related_transfer_guid_list[0]))
	print "Transfer GUID:", transfer_guid
	# Get transfer info from base wallet
	sent_transfer_info = 0
	while True:
		try:
			sent_transfer_info = http_client.GetSentTransferInfo20151004(base_wallet_guid,transfer_guid)
			break
		except pyjsonrpc.rpcerror.JsonRpcError as e:
			error_code = EGeneralWalletLocalApiErrorCodes20151004(e.code)
			if error_code == EGeneralWalletLocalApiErrorCodes20151004.InQueueRetryLater:
				time.sleep(1) # wait ultil data loading
				continue
			else:
				raise
	if sent_transfer_info == "null":
		return
	print "Actual fee:", sent_transfer_info["Fee"]
	print "Sent tranfer info:", sent_transfer_info
	# Get transfer info from second wallet
	received_transfer_info = 0
	while True:
		try:
			received_transfer_info = http_client.GetReceivedTransferInfo20151004(second_wallet_guid,transfer_guid)
			break
		except pyjsonrpc.rpcerror.JsonRpcError as e:
			error_code = EGeneralWalletLocalApiErrorCodes20151004(e.code)
			if error_code == EGeneralWalletLocalApiErrorCodes20151004.InQueueRetryLater:
				time.sleep(1) # wait ultil data loading
				continue
			else:
				raise
	if received_transfer_info == "null":
		return
	print "Received transfer info:", received_transfer_info
	# Send some transfer back from second wallet
	request_guid = str(uuid.uuid4())
	sent_time_string = datetime.datetime.utcnow().strftime('%Y%m%d%H%M%S')
	transfer_amount = 8
	# estimate fee
	expected_fee = http_client.EstimateFee20151004(second_wallet_guid, base_wallet_guid, transfer_amount, transfer_comment_bytes_len, anonymous_transfer)
	print "2Expected fee:", expected_fee
	# calc hmac signature
	hmac_signature_b64 = ""
	if hmac_signature_required == True:
		hmac_signature_b64 = calc_hmac_code(request_guid, sent_time_string, second_wallet_guid, base_wallet_guid,  transfer_amount, anonymous_transfer, transfer_comment_bytes_b64, expected_fee, hmac_code_b64)
	same_request_guid = http_client.SendTransfer20151004(request_guid, second_wallet_guid, base_wallet_guid, transfer_amount, sent_time_string, anonymous_transfer, transfer_comment_bytes_b64, expected_fee, hmac_signature_b64)
	print "2Request GUID:", same_request_guid
	# wait ultil request processed
	recheck_request_status = False
	while True:
		try:
			request_status_response = http_client.GetSentTransferRequestStatus20151004(second_wallet_guid, request_guid, recheck_request_status)
			request_status = ESentRequestStatus20151004(request_status_response["Status"])
			if (request_status == ESentRequestStatus20151004.PreparedToSend) or (request_status == ESentRequestStatus20151004.NotFound):
				time.sleep(1)
				recheck_request_status = True
			else:
				break
		except pyjsonrpc.rpcerror.JsonRpcError as e:
			error_code = EGeneralWalletLocalApiErrorCodes20151004(e.code)
			if error_code == EGeneralWalletLocalApiErrorCodes20151004.InQueueRetryLater:
				time.sleep(1) # wait ultil data loading
				recheck_request_status = False
				continue
			else:
				raise
	print "2Request status:", request_status
	if(request_status != ESentRequestStatus20151004.Sent):
		return
	related_transfer_guid_list = request_status_response["RelatedTransferGuidList"]
	transfer_guid = str(uuid.UUID(related_transfer_guid_list[0]))
	print "2Transfer GUID:", transfer_guid
	# Try to get all transfers in time range with data cursors
	now_time_utc = datetime.datetime.utcnow()
	start_time = now_time_utc - datetime.timedelta(0, 60*10) # -10 minutes
	end_time = now_time_utc + datetime.timedelta(0, 60*5) # +5 minutes
	fetch_sent_transfers = True
	fetch_received_transfers = True
	stay_online = False
	cursor_num = http_client.CreateTransferDataCursor20151004(base_wallet_guid, start_time.strftime('%Y%m%d%H%M%S'), end_time.strftime('%Y%m%d%H%M%S'), fetch_sent_transfers, fetch_received_transfers, stay_online)
	print "Cursor num:", cursor_num
	cursor_status_response = 0
	cursor_status = 0
	while True:
		cursor_status_response = http_client.GetTransferDataCursorStatus20151004(cursor_num)
		cursor_status = ETransferDataCursorStatus20151004(cursor_status_response["Status"])
		if cursor_status != ETransferDataCursorStatus20151004.Fetching:
			break
		time.sleep(1) # wait ultil data loading
	total_transfers_in_cursor_count = cursor_status_response["TotalTransferCount"]
	print "Total transfers in cursor:", total_transfers_in_cursor_count
	offset = 0
	transfer_list = http_client.FetchTransferFromDataCursor20151004(cursor_num, offset, total_transfers_in_cursor_count)
	print "Transfer list:", transfer_list
	http_client.CloseTransferDataCursor20151004(cursor_num)
	
#test_proxy_api()
test_wallet_api()