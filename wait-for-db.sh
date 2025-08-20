#!/bin/bash
set -e

host="$1"
shift
cmd="$@"

until nc -z $host 1521; do
  echo "Oracle ainda não está pronto em $host:1521 - aguardando..."
  sleep 5
done

echo "Oracle está pronto! Executando comando..."
exec $cmd
