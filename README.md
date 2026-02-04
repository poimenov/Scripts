# Scripts

RadioBrowser Script screen:
![Screenshot of the script UI](/img/radioBrowser.Script.jpg)

Z3audio Script screen:
![Screenshot of the script UI](/img/z3audio.Script.jpg)

## Description

RadioBrowser script for playing radio stations from [radio browser](https://www.radio-browser.info/)

Z3audio script for searching, listening and downloading music from the site "z3.fm"

### Prerequisites

.Net SDK and FFMpeg

### Installation

Clone the repo:

```bash
git clone https://github.com/poimenov/Scripts.git
```

To run RadioBrowser script:

```bash
dotnet fsi RadioBrowser.fsx
```
To run Z3audio script:

```bash
dotnet fsi z3audio.fsx
```

If you run into problems in linux, you may need to install vlc and vlc dev related libraries

```bash
apt-get install libvlc-dev.
apt-get install vlc
```

If you still have issues you can refer to this [guide](https://code.videolan.org/videolan/LibVLCSharp/blob/3.x/docs/linux-setup.md)

```bash
sudo apt install libx11-dev
```

I tested this script on Ubuntu 24.04 with .Net SDK 10.0
