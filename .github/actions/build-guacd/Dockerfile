#
# Dockerfile for guacamole-server
#

# The Alpine Linux image that should be used as the basis for the guacd image
# NOTE: Using 3.18 because the required openssl1.1-compat-dev package was
# removed in more recent versions.
ARG ALPINE_BASE_IMAGE=3.18

# build directory inside the container
ARG BUILD_DIR=/tmp/guacamole-server

# installation directory inside the container
ARG PREFIX_DIR=/opt/guacamole

# branch, tag, or commit of guacamole-server to build
ARG GUACAMOLE_SERVER_VERSION=1.6.0

# major version of FreeRDP to build
ARG FREERDP_VERSION=2

# autobuild.sh will use this to determine which version of freerdp to pull from GitHub
ARG WITH_FREERDP="${FREERDP_VERSION}(\.\d+)+"

ARG FREERDP_OPTS="\
    -DBUILTIN_CHANNELS=OFF \
    -DCHANNEL_URBDRC=OFF \
    -DWITH_ALSA=OFF \
    -DWITH_CAIRO=ON \
    -DWITH_CHANNELS=ON \
    -DWITH_CLIENT=ON \
    -DWITH_CUPS=OFF \
    -DWITH_DIRECTFB=OFF \
    -DWITH_FFMPEG=OFF \
    -DWITH_FUSE=OFF \
    -DWITH_GSM=OFF \
    -DWITH_GSSAPI=OFF \
    -DWITH_IPP=OFF \
    -DWITH_JPEG=ON \
    -DWITH_KRB5=ON \
    -DWITH_LIBSYSTEMD=OFF \
    -DWITH_MANPAGES=OFF \
    -DWITH_OPENH264=OFF \
    -DWITH_OPENSSL=ON \
    -DWITH_OSS=OFF \
    -DWITH_PCSC=OFF \
    -DWITH_PKCS11=OFF \
    -DWITH_PULSE=OFF \
    -DWITH_SERVER=OFF \
    -DWITH_SERVER_INTERFACE=OFF \
    -DWITH_SHADOW_MAC=OFF \
    -DWITH_SHADOW_X11=OFF \
    -DWITH_SWSCALE=OFF \
    -DWITH_WAYLAND=OFF \
    -DWITH_X11=OFF \
    -DWITH_X264=OFF \
    -DWITH_XCURSOR=ON \
    -DWITH_XEXT=ON \
    -DWITH_XI=OFF \
    -DWITH_XINERAMA=OFF \
    -DWITH_XKBFILE=ON \
    -DWITH_XRENDER=OFF \
    -DWITH_XTEST=OFF \
    -DWITH_XV=OFF \
    -DWITH_ZLIB=ON"

ARG GUACAMOLE_SERVER_OPTS="\
    --disable-guaclog \
    CPPFLAGS=-Wno-error=deprecated-declarations"

# -----------------------------------------------------------------------------
# Stage 1: Build the base image used for building guacd
# -----------------------------------------------------------------------------

FROM alpine:${ALPINE_BASE_IMAGE} AS builder
ARG BUILD_DIR
ARG GUACAMOLE_SERVER_VERSION

# install build dependencies
RUN apk add --no-cache         \
        autoconf               \
        automake               \
        build-base             \
        cairo-dev              \
        cjson-dev              \
        cmake                  \
        cunit-dev              \
        git                    \
        grep                   \
        krb5-dev               \
        libjpeg-turbo-dev      \
        libpng-dev             \
        libtool                \
        libwebp-dev            \
        make                   \
        openssl1.1-compat-dev  \
        pango-dev              \
        pulseaudio-dev         \
        sdl2-dev               \
        sdl2_ttf-dev           \
        util-linux-dev         \
        webkit2gtk-dev

# Pull guacamole-server source code and remove .git
RUN git clone --depth 1 --branch "${GUACAMOLE_SERVER_VERSION}" \
    https://github.com/apache/guacamole-server.git ${BUILD_DIR} && \
    rm -rf ${BUILD_DIR}/.git

# -----------------------------------------------------------------------------
# Stage 2: Build FreeRDP
# -----------------------------------------------------------------------------

FROM builder AS freerdp
ARG BUILD_DIR
ARG FREERDP_OPTS
ARG PREFIX_DIR
ARG WITH_FREERDP

RUN ${BUILD_DIR}/src/guacd-docker/bin/autobuild.sh "FREERDP" \
    "https://github.com/FreeRDP/FreeRDP"

# -----------------------------------------------------------------------------
# Stage 3: Build guacamole-server
# -----------------------------------------------------------------------------

FROM builder AS guacamole-server
ARG BUILD_DIR
ARG FREERDP_VERSION
ARG GUACAMOLE_SERVER_OPTS
ARG PREFIX_DIR

# bring in FreeRDP build from previous stage
COPY --from=freerdp ${PREFIX_DIR} ${PREFIX_DIR}
ARG FREERDP_LIB_PATH=${PREFIX_DIR}/lib/freerdp${FREERDP_VERSION}

# build guacd
RUN ${BUILD_DIR}/src/guacd-docker/bin/autobuild.sh "GUACAMOLE_SERVER" "${BUILD_DIR}"

# Record the packages of all runtime library dependencies
RUN ${BUILD_DIR}/src/guacd-docker/bin/list-dependencies.sh  \
    ${PREFIX_DIR}/sbin/guacd                                \
    ${PREFIX_DIR}/lib/libguac-client-*.so                   \
    ${FREERDP_LIB_PATH}/*guac*.so                           \
    > ${PREFIX_DIR}/DEPENDENCIES

# -----------------------------------------------------------------------------
# Stage 4: Build runtime image
# -----------------------------------------------------------------------------

# Use same Alpine version as the base for the runtime image
FROM alpine:${ALPINE_BASE_IMAGE} AS runtime
ARG PREFIX_DIR
ARG BUILD_DIR
ARG GUACAMOLE_SERVER_VERSION

# copy build artifacts into this stage
COPY --from=guacamole-server ${PREFIX_DIR} ${PREFIX_DIR}

# install/update runtime dependencies
RUN apk add --no-cache                \
        ca-certificates               \
        netcat-openbsd                \
        shadow                        \
        util-linux-login && \
    xargs apk add --no-cache < ${PREFIX_DIR}/DEPENDENCIES

# use UTF-8
ENV LC_ALL=C.UTF-8

# specify library path
ENV LD_LIBRARY_PATH=${PREFIX_DIR}/lib

# every 5 minutes, check that guacd is still running
HEALTHCHECK --interval=5m --timeout=5s CMD nc -z 127.0.0.1 4822 || exit 1

## write wsl distribution config
RUN /bin/sh -c 'cat > /etc/wsl-distribution.conf <<WSLCONF \
&& echo "[oobe]"                                                        >> /etc/wsl-distribution.conf \
&& echo "command = cd /opt/guacamole"                                   >> /etc/wsl-distribution.conf \
&& echo "defaultName = guacd-${GUACAMOLE_SERVER_VERSION}"               >> /etc/wsl-distribution.conf \
&& echo "[user]"                                                        >> /etc/wsl-distribution.conf \
&& echo "default = guacd"                                               >> /etc/wsl-distribution.conf \
&& echo "[boot]"                                                        >> /etc/wsl-distribution.conf \
&& echo "command = cd /opt/guacamole && /opt/guacamole/entrypoint.sh"   >> /etc/wsl-distribution.conf \
&& echo "[automount]"                                                   >> /etc/wsl-distribution.conf \
&& echo "enabled = false"                                               >> /etc/wsl-distribution.conf \
&& echo "[interop]"                                                     >> /etc/wsl-distribution.conf \
&& echo "enabled = false"                                               >> /etc/wsl-distribution.conf \
&& echo "[shortcut]"                                                    >> /etc/wsl-distribution.conf \
&& echo "enabled = false"                                               >> /etc/wsl-distribution.conf \
&& echo "[windowsterminal]"                                             >> /etc/wsl-distribution.conf \
&& echo "enabled = false"                                               >> /etc/wsl-distribution.conf'

# create runtime user
ARG UID=1000
ARG GID=1000
RUN groupadd --gid $GID guacd
RUN useradd --system --create-home --shell /sbin/nologin --uid $UID --gid $GID guacd
USER guacd

# expose the guacd port
EXPOSE 4822

COPY --from=guacamole-server ${BUILD_DIR}/src/guacd-docker/bin/entrypoint.sh /opt/guacamole/
ENTRYPOINT [ "/opt/guacamole/entrypoint.sh" ]
