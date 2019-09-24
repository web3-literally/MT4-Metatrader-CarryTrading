class Users {
  constructor () {
     this.users = [];
  }

  addUser (socket, userid) {
    const user = { socket, userid };
    this.users.push(user);
    return user;
  }

  removeUserFromSocket (socket) {
    const user = this.getUserFromSocket(socket);

    if (user) {
      this.users = this.users.filter((user) => user.socket != socket);
    }
    return user;
  }

  getUserFromSocket (socket) {
    return this.users.filter((user) => user.socket == socket)[0];
  }

  removeUserFromUserId (userid) {
    const user = this.getUserFromUserId(userid);

    if (user) {
      this.users = this.users.filter((user) => user.userid != userid);
    }
    return user;
  }

  getUserFromUserId (userid) {
    return this.users.filter((user) => user.userid == userid)[0];
  }
}

module.exports = { Users };
