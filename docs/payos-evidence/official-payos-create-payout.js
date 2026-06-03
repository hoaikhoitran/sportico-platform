require('dotenv').config();
const fetch = require('node-fetch'); // npm install node-fetch
const { v4: uuidv4 } = require('uuid'); // npm install uuid
const { createSignature } = require('./lib/signature');

// Đọc biến môi trường
const clientId = process.env.CLIENT_ID;
const apiKey = process.env.API_KEY;
const checksumKey = process.env.CHECKSUM_KEY;

if (!clientId || !apiKey || !checksumKey) {
  console.error('Thiếu biến môi trường. Vui lòng kiểm tra CLIENT_ID, API_KEY, CHECKSUM_KEY trong file .env');
  process.exit(1);
}

// Dữ liệu chuyển tiền mẫu
const payoutData = {
  referenceId: 'payout_' + Date.now(),
  amount: 2000,
  description: 'payout',
  toBin: '970422',
  toAccountNumber: '0973592402',
  category: ['salary', 'hoa'],
};

// Tạo signature
const signature = createSignature(checksumKey, payoutData);

// Sinh UUID cho idempotency key
const idempotencyKey = uuidv4();

// Tạo headers
const myHeaders = {
  'x-idempotency-key': idempotencyKey,
  'x-client-id': clientId,
  'x-api-key': apiKey,
  'x-signature': signature,
  'Content-Type': 'application/json',
};

// Gửi request
fetch('https://api-merchant.payos.vn/v1/payouts', {
  method: 'POST',
  headers: myHeaders,
  body: JSON.stringify(payoutData),
})
  .then((response) => response.json())
  .then((result) => {
    console.log('Kết quả:', result);
  })
  .catch((error) => {
    console.error('Lỗi:', error);
  }); 