"""SFTP front for the mock mailbox: username + password login, chrooted to config.MAILBOX."""

import asyncssh

from . import config


class _SSHServer(asyncssh.SSHServer):
    def begin_auth(self, username: str) -> bool:
        return True  # auth is required (password below)

    def password_auth_supported(self) -> bool:
        return True

    def validate_password(self, username: str, password: str) -> bool:
        return username == config.SFTP_USER and password == config.SFTP_PASSWORD


async def start() -> asyncssh.SSHAcceptor:
    if not config.HOST_KEY.exists():
        asyncssh.generate_private_key("ssh-ed25519").write_private_key(str(config.HOST_KEY))
    return await asyncssh.listen(
        "", config.SFTP_PORT,
        server_factory=_SSHServer,
        server_host_keys=[str(config.HOST_KEY)],
        sftp_factory=lambda chan: asyncssh.SFTPServer(chan, chroot=str(config.MAILBOX)),
    )
