FROM chromedp/headless-shell:latest

ENTRYPOINT ["/headless-shell/headless-shell", "--no-sandbox", "--disable-gpu", "—-timeout=50000", "--remote-debugging-address=0.0.0.0", "--remote-debugging-port=9222","--proxy-server=proxy.dev:1234"]
