self.addEventListener("push", (event) => {
  let data = { title: "Rastreador Veicular", body: "Novo alerta" };
  if (event.data) {
    try {
      data = event.data.json();
    } catch {
      data.body = event.data.text();
    }
  }

  event.waitUntil(self.registration.showNotification(data.title, { body: data.body }));
});
