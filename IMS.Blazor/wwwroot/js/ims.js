// Small browser helpers called from Blazor (C#) through IJSRuntime.
window.imsPdf = {
  // bytes (Uint8Array) -> a temporary address the <iframe> can show
  createUrl: function (bytes, contentType) {
    var blob = new Blob([bytes], { type: contentType });
    return URL.createObjectURL(blob);
  },

  revoke: function (url) {
    URL.revokeObjectURL(url);
  },

  print: function (frameId) {
    var frame = document.getElementById(frameId);

    if (frame && frame.contentWindow) {
      frame.contentWindow.focus();
      frame.contentWindow.print();
    }
  },

  download: function (url, fileName) {
    var link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
  }
};
