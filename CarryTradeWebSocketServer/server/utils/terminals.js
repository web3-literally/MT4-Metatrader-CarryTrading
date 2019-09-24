class Terminals {
  constructor () {
     this.terminals = [];
  }

  addTerminal (socket, terminalid) {
    const terminal = { socket, terminalid};
    this.terminals.push(terminal);
    return terminal;
  }

  removeTerminalFromSocket (socket) {
    const terminal = this.getTerminalFromSocket(socket);

    if (terminal) {
      this.terminals = this.terminals.filter((terminal) => terminal.socket != socket);
    }
    return terminal;
  }

  getTerminalFromSocket (socket) {
    return this.terminals.filter((terminal) => terminal.socket == socket)[0];
  }

  removeTerminalFromTerminalId (terminalid) {
    const terminal = this.getTerminalFromTerminalId(terminalid);

    if (terminal) {
      this.terminals = this.terminals.filter((terminal) => terminal.terminalid != terminalid);
    }
    return terminal;
  }

  getTerminalFromTerminalId (terminalid) {
    return this.terminals.filter((terminal) => terminal.terminalid == terminalid)[0];
  }
}

module.exports = { Terminals };
