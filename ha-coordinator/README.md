## Deploy

```bash
# use ha=false to deploy without high availability(2 machines), we will scale if needed
# If you only want one machine, use fly scale count 1 and fly deploy --ha=false https://community.fly.io/t/is-it-possible-to-set-a-maximum-number-of-machines/15193
fly deploy --ha=false
```
