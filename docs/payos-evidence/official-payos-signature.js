/**
 * Sắp xếp sâu object (deep sort) để chuẩn hóa dữ liệu trước khi ký
 */
function deepSortObj(obj, sortArrays = false) {
  return Object.keys(obj)
    .sort()
    .reduce((acc, key) => {
      const value = obj[key];
      if (Array.isArray(value)) {
        if (sortArrays) {
          acc[key] = value
            .map((item) =>
              typeof item === 'object' && item !== null ? deepSortObj(item, sortArrays) : item,
            )
            .sort((a, b) => {
              if (typeof a !== 'object' && typeof b !== 'object') {
                return String(a).localeCompare(String(b));
              }
              return JSON.stringify(a).localeCompare(JSON.stringify(b));
            });
        } else {
          acc[key] = value.map((item) =>
            typeof item === 'object' && item !== null ? deepSortObj(item, sortArrays) : item,
          );
        }
      } else if (typeof value === 'object' && value !== null) {
        acc[key] = deepSortObj(value, sortArrays);
      } else {
        acc[key] = value;
      }
      return acc;
    }, {});
}

/**
 * Tạo signature HMAC SHA-256 (Node.js)
 */
function createSignature(secretKey, jsonData) {
  const crypto = require('crypto');
  const sortedData = deepSortObj(jsonData, false);
  const queryString = Object.keys(sortedData)
    .map((key) => {
      let value = sortedData[key];
      if (Array.isArray(value) || (typeof value === 'object' && value !== null)) {
        value = JSON.stringify(value);
      }
      if (value === null || value === undefined) value = '';
      return `${encodeURIComponent(key)}=${encodeURIComponent(String(value))}`;
    })
    .join('&');
  return crypto.createHmac('sha256', secretKey).update(queryString).digest('hex');
}

module.exports = { deepSortObj, createSignature }; 