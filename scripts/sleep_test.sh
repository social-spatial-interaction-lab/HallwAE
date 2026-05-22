#!/bin/bash
# Polls Quest headset wake state every 10s and logs when it falls asleep.

INTERVAL=10

echo "Sleep test started at $(date '+%H:%M:%S'). Polling every ${INTERVAL}s. Ctrl+C to stop."
echo ""

START=$(date +%s)
LAST_STATE=""

while true; do
    STATE=$(adb shell dumpsys power 2>/dev/null | grep "mWakefulness=" | tr -d ' \r' | cut -d= -f2)

    if [ -z "$STATE" ]; then
        echo "$(date '+%H:%M:%S')  [no device / ADB error]"
    elif [ "$STATE" != "$LAST_STATE" ]; then
        NOW=$(date +%s)
        ELAPSED=$(( NOW - START ))
        MINS=$(( ELAPSED / 60 ))
        SECS=$(( ELAPSED % 60 ))
        echo "$(date '+%H:%M:%S')  → $STATE  (elapsed: ${MINS}m ${SECS}s)"
        LAST_STATE="$STATE"
    else
        echo "$(date '+%H:%M:%S')  $STATE"
    fi

    sleep $INTERVAL
done
