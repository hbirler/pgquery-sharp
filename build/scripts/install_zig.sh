#!/usr/bin/env bash
set -eux;
ZIG_VERSION=${1:?"usage: install_zig.sh <zig_version> <sha256sum>"}
ZIG_SHA256SUM=${2:?"usage: install_zig.sh <zig_version> <sha256sum>"}
ZIG_PUBKEY='RWSGOq2NVecA2UPNdBUZykf1CCb147pkmdtYxgb3Ti+JO/wCYvhbAb/U';
MIRRORS_URL='https://ziglang.org/download/community-mirrors.txt';
# Fetch mirror list (newline-separated)
curl -fsSL "${MIRRORS_URL}" > /tmp/zig_mirrors.txt;
# Shuffle to avoid hammering the same mirror
shuf /tmp/zig_mirrors.txt > /tmp/zig_mirrors_shuffled.txt;
# Add ziglang.org as final fallback
printf '%s\n' 'https://ziglang.org/download' >> /tmp/zig_mirrors_shuffled.txt;
SUCCESS=0;
for MIRROR in $(cat /tmp/zig_mirrors_shuffled.txt); do
    MIRROR="${MIRROR%/}";
    BASE_URL="${MIRROR}/${ZIG_VERSION}";
    TARBALL_URL="${BASE_URL}/zig-x86_64-linux-${ZIG_VERSION}.tar.xz";
    SIG_URL="${TARBALL_URL}.minisig";
    echo "Trying ${TARBALL_URL}";
    rm -f /tmp/zig.tar.xz /tmp/zig.tar.xz.minisig || true;
    if curl -fsSL -o /tmp/zig.tar.xz "${TARBALL_URL}" && curl -fsSL -o /tmp/zig.tar.xz.minisig "${SIG_URL}"; then
        echo "Downloaded from ${MIRROR}, verifying minisign signature...";
        if minisign -Vm /tmp/zig.tar.xz -P "${ZIG_PUBKEY}" -x /tmp/zig.tar.xz.minisig; then
            echo "Signature OK from ${MIRROR}";
            SUCCESS=1;
            break;
        else
            echo "Signature verification FAILED for ${MIRROR}, trying next..." >&2;
        fi;
    else
        echo "Download failed from ${MIRROR}, trying next..." >&2;
    fi;
done;
test "${SUCCESS}" -eq 1;
echo "${ZIG_SHA256SUM}  /tmp/zig.tar.xz" | sha256sum -c -;
tar -xJf /tmp/zig.tar.xz --strip-components=1 -C /usr/local;
ln -s /usr/local/zig /usr/local/bin/zig;
rm -f /tmp/zig.tar.xz /tmp/zig.tar.xz.minisig /tmp/zig_mirrors*.txt