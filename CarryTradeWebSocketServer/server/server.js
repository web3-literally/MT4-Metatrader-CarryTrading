const path = require('path');
const http = require('http');
const express = require('express');
const webSocket = require('ws');
const { RegisterTable } = require('./utils/register_table');
const { Users } = require('./utils/users');
const { Terminals } = require('./utils/terminals');

//Server Setting
const app = express();
const server = http.createServer(app);
const io = new webSocket.Server({server});

const publicPath = path.join(__dirname, '../public');
const port = process.env.PORT || 3000;
app.use(express.static(publicPath));

//Main Class
const users = new Users();
const terminals = new Terminals();
const register_table = new RegisterTable();

register_table.addPair('d30fa5a6-4560-44d7-879b-b8cbc01088a4','ApolloTest1');

//Main Function
function covertToEmitData(typeVal, dataVal){
  var msg = {
    type: typeVal,
    data: dataVal
  }
  return (JSON.stringify(msg));
}

function getTerminalSocketFromUserSocket(socket) {
  user = users.getUserFromSocket(socket);
  if (!user) return null;

  pair = register_table.getPairFromUserId(user.userid);
  if (!pair) return null;

  terminal = terminals.getTerminalFromTerminalId(pair.terminal_id);
  if (!terminal) return null;

  var retClient = null;
  io.clients.forEach(function each(client) {
    if (client.readyState === webSocket.OPEN) {
      if (client == terminal.socket)
      {
        retClient = client;
      }
    }
  });
  return retClient;
}

function getUserSocketFromTerminalSocket(socket) {
  //console.log('getUserSocketFromTerminalSocket => ');
  terminal = terminals.getTerminalFromSocket(socket);
  if (!terminal) return null;

  pair = register_table.getPairFromTerminalId(terminal.terminalid);
  if (!pair) return null;

  user = users.getUserFromUserId(pair.user_id);
  if (!user) return null;

  var retClient = null;
  io.clients.forEach(function each(client) {
    if (client.readyState === webSocket.OPEN) {
      if (client == user.socket)
      {
        retClient = client;
      }
    }
  });
  return retClient;
}

io.on('connection', (socket) => {
  console.log('New user connected');

  //MESSAGE EVENT
  socket.onmessage = function(event, callback) {
    var msg = JSON.parse(event.data);
    var type = msg.type;
    var command = msg.command;
    var data = msg.data;

    console.log(msg);

    switch (type) {
      case 'joinUser'://When User Connected!
        params = data;
        users.removeUserFromSocket(socket);
        users.addUser(socket, params.userid);
        pair = register_table.getPairFromUserId(params.userid);
        console.log(pair);
        if (!pair) return;

        terminal = terminals.getTerminalFromTerminalId(pair.terminal_id);
        if (!terminal) return;

        socket.send(covertToEmitData('terminalid', terminal.terminalid));
        getTerminalSocketFromUserSocket(socket).send(covertToEmitData('userid', params.userid));
        break;

      case 'joinTerminal'://When Terminal Connected!
        params = data;
        terminals.removeTerminalFromSocket(socket);
        terminals.addTerminal(socket, params.terminalid);
        pair = register_table.getPairFromTerminalId(params.terminalid);
        console.log(pair);
        if (!pair) return;
        user = users.getUserFromUserId(pair.user_id);
        if (!user) return;

        socket.send(covertToEmitData('userid', user.userid));
        getUserSocketFromTerminalSocket(socket).send(covertToEmitData('terminalid', params.terminalid));
        console.log(getUserSocketFromTerminalSocket(socket));
        break;

      case 'SendDataFromUserToTerminal'://When SendData From User To Terminal!
        console.log('SendDataFromUserToTerminal:');
        if (!getTerminalSocketFromUserSocket(socket)) return;

        params = data;

        console.log(command);
        console.log(params);
        getTerminalSocketFromUserSocket(socket).send(covertToEmitData(command, params));
        break;

      case 'SendDataFromTerminalToUser'://When SendData From Terminal To User!
        console.log('SendDataFromTerminalToUser:');
        if (!getUserSocketFromTerminalSocket(socket)) return;

        params = data;

        console.log(command);
        console.log(params);
        getUserSocketFromTerminalSocket(socket).send(covertToEmitData(command, params));
        break;
    }
  }

  //SOCKET CLOSE EVENT
  socket.onclose = function(event, callback){
    console.log('user disconnected');

    var user = users.getUserFromSocket(socket);
    var terminal = terminals.getTerminalFromSocket(socket);

    if (getTerminalSocketFromUserSocket(socket))
    {
      getTerminalSocketFromUserSocket(socket).send(covertToEmitData("DisconnectedUser",""));
    }
    if (getUserSocketFromTerminalSocket(socket))
    {
      getUserSocketFromTerminalSocket(socket).send(covertToEmitData("DisconnectedTerminal",""));
    }

    users.removeUserFromSocket(socket);
    terminals.removeTerminalFromSocket(socket);
  }
});

server.listen(port, () => {
  console.log(`Server is up and running on port ${port}`);
});
