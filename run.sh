#!/usr/bin/env bash
DOCKER_BUILDKIT=1 docker build -f build/Dockerfile --target out --output type=local,dest=./out .
