const socket = new WebSocket('ws://localhost:3000');

function scrollToBottom() {
  const messages = jQuery('#messages');
  const newMessage = messages.children('li:last-child');

  const clientHeight = messages.prop('clientHeight');
  const scrollTop = messages.prop('scrollTop');
  const scrollHeight = messages.prop('scrollHeight');
  const newMessageHeight = newMessage.innerHeight();
  const lastMessageHeight = newMessage.prev().innerHeight();

  if (clientHeight + scrollTop + newMessageHeight + lastMessageHeight >= scrollHeight) {
    messages.scrollTop(scrollHeight);
  }
};

function socket_emit(typeVal, dataVal){
  var msg = {
    type: typeVal,
    data: dataVal
  }
  socket.send(JSON.stringify(msg));
}

socket.onopen = function () {
  const params = jQuery.deparam(window.location.search);
  socket_emit ('joinUser', params);
}

socket.onclose = function () {
  console.log('Disconnected from the server');
}

socket.onmessage = function(event){
  var msg = JSON.parse(event.data);
  var type = msg.type;
  var data = msg.data;
  switch (type) {
    case 'updateUserList':
      var users = data;
      const ol = jQuery('<ol></ol>');
      users.forEach(function (user) {
        ol.append(jQuery('<li></li>').text(user));
      });
      jQuery('#users').html(ol);
      break;

    case 'newMessage':
      var message = data;
      const formattedTime = moment(message.createdAt).format('h:mm a');
      const template = jQuery('#message-template').html();
      const html = Mustache.render(template, {
        text: message.text,
        from: message.from,
        createdAt: formattedTime,
      });
      jQuery('#messages').append(html);
      scrollToBottom();
      break;
  }
}

jQuery('#message-form').on('submit', function (e) {
  e.preventDefault();
  const messageTextBox = jQuery('[name=message]');

  socket_emit('createMessageUser', {
    text: messageTextBox.val()
  });
  messageTextBox.val('')
});
