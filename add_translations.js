const fs = require('fs');

const trPath = './frontend/lib/public/locales/tr.json';
const enPath = './frontend/lib/public/locales/en.json';

const tr = JSON.parse(fs.readFileSync(trPath, 'utf8'));
const en = JSON.parse(fs.readFileSync(enPath, 'utf8'));

tr.policies['App.RDP.StripSignatures'] = {
  title: 'RDP İmzalarını Temizle (Pano ve Sürücüleri Serbest Bırak)',
  help: 'Etkinleştirildiğinde, merkezi yayımlama sunucusundan (RDS Broker) gelen RDP bağlantılarındaki dijital imzalar silinir. Bu sayede kullanıcılar bağlantı öncesinde Pano (Kopyala/Yapıştır), Yazıcılar ve Yerel Sürücüler gibi seçenekleri özgürce seçip değiştirebilirler.'
};

en.policies['App.RDP.StripSignatures'] = {
  title: 'Strip RDP Signatures (Unlock Clipboard and Drives)',
  help: 'When enabled, digital signatures from Centralized Publishing (RDS Broker) are stripped from the RDP connection. This allows users to freely check and uncheck options like Clipboard, Printers, and Local Drives before connecting.'
};

fs.writeFileSync(trPath, JSON.stringify(tr, null, 2));
fs.writeFileSync(enPath, JSON.stringify(en, null, 2));
