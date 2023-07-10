export var editor = ace.edit("editor");
editor.setFontSize(14);
var JavaScriptMode = ace.require("ace/mode/javascript").Mode;
editor.session.setMode(new JavaScriptMode());




