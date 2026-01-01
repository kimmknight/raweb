import http from 'http';
import readline from 'readline';
import { createServer as createViteServer } from 'vite';

async function start() {
  // start vite dev server, which uses HTTPS
  const vite = await createViteServer({
    server: {
      host: true,
      port: 5174,
    },
  });
  const { logger } = vite.config;
  await vite.listen();

  logger.info('Development server is running', { timestamp: true });
  const printHttpUrl = () => {
    logger.info(
      '  \u001b[32m➜\u001b[39m  \u001b[1mLocal\u001b[22m:   \u001b[36mhttp://localhost:\u001b[1m5173\u001b[36m/\u001b[39m'
    );
  };
  printHttpUrl();
  vite.printUrls();

  // attach stdin listener like the vite CLI does
  readline.emitKeypressEvents(process.stdin);
  if (process.stdin.isTTY) process.stdin.setRawMode(true);

  // press r + enter to restart the server
  // press u + enter to show server url
  // press o + enter to open in browser
  // press c + enter to clear console
  // press q + enter to quit

  let buffer = '';
  process.stdin.on('keypress', (str, key) => {
    if (key.name === 'return') {
      readline.clearLine(process.stdout, 0);
      readline.cursorTo(process.stdout, 0);

      if (buffer === 'h') {
        logger.info('\u001b[1mCommands\u001b[22m:');
        logger.info('\u001b[32m\u001b[1mr + enter\u001b[22m\u001b[39m  restart the server');
        logger.info('\u001b[32m\u001b[1mu + enter\u001b[22m\u001b[39m  show server url');
        logger.info('\u001b[32m\u001b[1mo + enter\u001b[22m\u001b[39m  open in browser');
        logger.info('\u001b[32m\u001b[1mc + enter\u001b[22m\u001b[39m  clear console');
        logger.info('\u001b[32m\u001b[1mq + enter\u001b[22m\u001b[39m  quit');
      }

      if (buffer === 'r') {
        logger.clearScreen('info');
        vite.restart();
      }

      if (buffer === 'u') {
        printHttpUrl();
        vite.printUrls();
      }

      if (buffer === 'o') {
        vite.openBrowser();
      }

      if (buffer === 'c') {
        logger.clearScreen('info');
      }

      if (buffer === 'q') {
        process.exit();
      }

      console.log('');
      buffer = '';
    } else if (!key.ctrl && !key.meta && !key.sequence.includes('\u0003')) {
      process.stdout.write(str);
      buffer += str;
    }

    if (key.ctrl && key.name === 'c') process.exit();
  });

  logger.info('  \u001b[32m➜\u001b[39m  press \u001b[1mh + enter\u001b[22m to show help.\n');

  // create a tiny HTTP redirector on port 5173
  http
    .createServer((req, res) => {
      const host = req.headers.host?.replace(/:\d+$/, ':5174') || 'localhost:5174';
      res.writeHead(301, { Location: `https://${host}${req.url}` });
      res.end();
    })
    .listen(5173);
}

start();
