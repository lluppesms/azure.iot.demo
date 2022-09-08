// https://docs.microsoft.com/en-us/aspnet/core/blazor/file-downloads?view=aspnetcore-6.0

async function downloadFileFromStream(fileName, contentStreamReference) {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);

    const url = URL.createObjectURL(blob);

    triggerFileDownload(fileName, url);

    URL.revokeObjectURL(url);
}

function triggerFileDownload(fileName, url) {
    const anchorElement = document.createElement('a');
    anchorElement.href = url;

    if (fileName) {
        anchorElement.download = fileName;
    }

    anchorElement.click();
    anchorElement.remove();
}

// used to call back into C#
var dotNetObject;

// called from the first render
window.getWindowDimensions = (obj) => {
    dotNetObject = obj; // save the object reference

    // return window size
    var size = {
        width: window.innerWidth,
        height: window.innerHeight
    };
    return size;
};

// When the window gets resized...
window.addEventListener('resize', function () {
    // we need to use dotNetObject
    if (dotNetObject != null) {
        // get the window size
        var size = {
            width: window.innerWidth,
            height: window.innerHeight
        };
        // call C# Resize function, passing the size
        dotNetObject.invokeMethodAsync('Resize', size);
    }
});

window.SetFocusToElement = (element) => {
    element.focus();
};

window.clipboardCopy = {
    copyText: function (textToCopy) {
        // navigator clipboard api needs a secure context to work (https)
        if (navigator.clipboard && window.isSecureContext) {
            return navigator.clipboard.writeText(textToCopy);
        } else {
            // use a hidden text area out of viewport to copy the data
            let textArea = document.createElement("textarea");
            textArea.value = textToCopy;
            textArea.style.position = "fixed";
            textArea.style.left = "-999999px";
            textArea.style.top = "-999999px";
            document.body.appendChild(textArea);
            textArea.focus();
            textArea.select();
            return new Promise((res, rej) => {
                document.execCommand('copy') ? res() : rej();
                textArea.remove();
            });
        }
    }
}
