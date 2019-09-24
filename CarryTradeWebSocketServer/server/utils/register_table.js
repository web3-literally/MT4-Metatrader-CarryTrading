class RegisterTable {
    constructor () {
        this.user_terminal_pairs = [];
    }

    addPair (user_id, terminal_id) {
        const pair = { user_id, terminal_id };
        this.user_terminal_pairs.push(pair);
        return pair;
    }

    getPairFromUserId (user_id) {
        return this.user_terminal_pairs.filter((pair) => pair.user_id == user_id)[0];
    }

    getPairFromTerminalId (terminal_id) {
        return this.user_terminal_pairs.filter((pair) => pair.terminal_id == terminal_id)[0];
    }

    removePairFromUserId (user_id) {
        const pair = this.getPairFromUserId(user_id);

        if (pair) {
            this.user_terminal_pairs = this.user_terminal_pairs.filter((pair) => pair.user_id != user_id);
        }
        return pair;
    }

    removePairFromTerminalId (terminal_id) {
        const pair = this.getPairFromTerminalId(terminal_id);

        if (pair) {
            this.user_terminal_pairs = this.user_terminal_pairs.filter((pair) => pair.terminal_id != terminal_id);
        }
        return pair;
    }
}

module.exports = { RegisterTable };
